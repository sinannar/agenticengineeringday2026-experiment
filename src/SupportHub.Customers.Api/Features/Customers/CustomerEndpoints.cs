using Microsoft.EntityFrameworkCore;

namespace SupportHub.Customers.Api.Features.Customers;

public static class CustomerEndpoints
{
    private const int MaximumTierBatchSize = 500;

    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/customers").WithTags("Customers");

        group.MapGet("/", ListAsync);
        group.MapPost("/support-tiers", GetSupportTiersAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> GetSupportTiersAsync(
        CustomerTierBatchRequest request,
        CustomerDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.CustomerIds is null || request.CustomerIds.Count is < 1 or > MaximumTierBatchSize)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["customerIds"] = [$"Customer IDs must contain between 1 and {MaximumTierBatchSize} values."]
            });
        }

        var customerIds = request.CustomerIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (customerIds.Length != request.CustomerIds.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["customerIds"] = ["Customer IDs must be non-empty and unique."]
            });
        }

        var tiers = await db.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .Select(customer => new CustomerTierResponse(customer.Id, customer.SupportTier))
            .ToListAsync(cancellationToken);

        return Results.Ok(tiers);
    }

    private static async Task<IResult> ListAsync(CustomerDbContext db, CancellationToken cancellationToken)
    {
        var customers = await db.Customers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new CustomerResponse(x.Id, x.Name, x.Email, x.PhoneNumber, x.SupportTier))
            .ToListAsync(cancellationToken);

        return Results.Ok(customers);
    }

    private static async Task<IResult> GetAsync(Guid id, CustomerDbContext db, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return customer is null ? Results.NotFound() : Results.Ok(CustomerValidation.ToResponse(customer));
    }

    private static async Task<IResult> CreateAsync(
        CustomerRequest request,
        CustomerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = CustomerValidation.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Email = string.Empty
        };
        CustomerValidation.Apply(customer, request);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/customers/{customer.Id}", CustomerValidation.ToResponse(customer));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        CustomerRequest request,
        CustomerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = CustomerValidation.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (customer is null)
        {
            return Results.NotFound();
        }

        CustomerValidation.Apply(customer, request);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(CustomerValidation.ToResponse(customer));
    }

    private static async Task<IResult> DeleteAsync(Guid id, CustomerDbContext db, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (customer is null)
        {
            return Results.NotFound();
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
