using System.Net.Mail;

namespace SupportHub.Customers.Api.Features.Customers;

public sealed record CustomerRequest(
    string? Name,
    string? Email,
    string? PhoneNumber,
    SupportTier SupportTier);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    SupportTier SupportTier);

public static class CustomerValidation
{
    public const int NameMaximumLength = 200;
    public const int EmailMaximumLength = 320;
    public const int PhoneMaximumLength = 50;

    public static Dictionary<string, string[]> Validate(CustomerRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > NameMaximumLength)
        {
            errors["name"] = [$"Name must be {NameMaximumLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else if (request.Email.Length > EmailMaximumLength || !MailAddress.TryCreate(request.Email, out _))
        {
            errors["email"] = ["Email must be a valid email address."];
        }

        if (request.PhoneNumber?.Length > PhoneMaximumLength)
        {
            errors["phoneNumber"] = [$"Phone number must be {PhoneMaximumLength} characters or fewer."];
        }

        if (!Enum.IsDefined(request.SupportTier))
        {
            errors["supportTier"] = ["Support tier must be Standard, Premium, or Enterprise."];
        }

        return errors;
    }

    public static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.PhoneNumber, customer.SupportTier);

    public static void Apply(Customer customer, CustomerRequest request)
    {
        customer.Name = request.Name!.Trim();
        customer.Email = request.Email!.Trim();
        customer.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        customer.SupportTier = request.SupportTier;
    }
}
