using Microsoft.EntityFrameworkCore;
using SupportHub.Tickets.Api.Features.Attachments;

namespace SupportHub.Tickets.Api.Features.Tickets;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/tickets").WithTags("Tickets");

        group.MapGet("", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        TicketDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var query = db.Tickets.AsNoTracking();

        if (!TryParseEnum(request.Query["status"], "status", errors, out TicketStatus? status))
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryParseEnum(request.Query["priority"], "priority", errors, out TicketPriority? priority))
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryParseGuid(request.Query["customerId"], "customerId", errors, out Guid? customerId))
        {
            return Results.ValidationProblem(errors);
        }

        var assignee = request.Query["assignee"].FirstOrDefault();
        if (assignee is not null && assignee.Length > TicketValidation.AssigneeMaximumLength)
        {
            errors["assignee"] = [$"Assignee must be {TicketValidation.AssigneeMaximumLength} characters or fewer."];
            return Results.ValidationProblem(errors);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (assignee is not null)
        {
            query = query.Where(x => x.Assignee == assignee);
        }

        var tickets = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.Id)
            .Select(x => TicketValidation.ToResponse(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(tickets);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        TicketDbContext db,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return ticket is null ? Results.NotFound() : Results.Ok(TicketValidation.ToResponse(ticket));
    }

    private static async Task<IResult> CreateAsync(
        TicketRequest request,
        TicketDbContext db,
        CustomerLookupClient customers,
        CancellationToken cancellationToken)
    {
        var errors = TicketValidation.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var customerResult = await customers.ExistsAsync(request.CustomerId, cancellationToken);
        if (customerResult != CustomerLookupResult.Exists)
        {
            return CustomerError(customerResult);
        }

        var now = DateTimeOffset.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        TicketValidation.Apply(ticket, request);

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/tickets/{ticket.Id}", TicketValidation.ToResponse(ticket));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        TicketRequest request,
        TicketDbContext db,
        CustomerLookupClient customers,
        CancellationToken cancellationToken)
    {
        var errors = TicketValidation.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var ticket = await db.Tickets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        if (ticket.CustomerId != request.CustomerId)
        {
            var customerResult = await customers.ExistsAsync(request.CustomerId, cancellationToken);
            if (customerResult != CustomerLookupResult.Exists)
            {
                return CustomerError(customerResult);
            }
        }

        TicketValidation.Apply(ticket, request);
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(TicketValidation.ToResponse(ticket));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        TicketDbContext db,
        IAttachmentBlobStore blobStore,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        var cleanupResult = await AttachmentEndpoints.CleanupForTicketAsync(
            id,
            db,
            blobStore,
            cancellationToken);
        if (cleanupResult is not null)
        {
            return cleanupResult;
        }

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static IResult CustomerError(CustomerLookupResult result) =>
        result == CustomerLookupResult.NotFound
            ? Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["customerId"] = ["The specified customer does not exist."]
            })
            : Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://supporthub.dev/problems/customer-service-unavailable",
                title: "Customer service unavailable",
                detail: "The customer service could not validate the customer reference.");

    private static bool TryParseEnum<TEnum>(
        string? value,
        string key,
        Dictionary<string, string[]> errors,
        out TEnum? result)
        where TEnum : struct, Enum
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Enum.TryParse<TEnum>(value, true, out var parsed) && Enum.IsDefined(parsed))
        {
            result = parsed;
            return true;
        }

        errors[key] = [$"{key} must be a valid {typeof(TEnum).Name} value."];
        return false;
    }

    private static bool TryParseGuid(
        string? value,
        string key,
        Dictionary<string, string[]> errors,
        out Guid? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            result = parsed;
            return true;
        }

        errors[key] = [$"{key} must be a valid customer ID."];
        return false;
    }
}
