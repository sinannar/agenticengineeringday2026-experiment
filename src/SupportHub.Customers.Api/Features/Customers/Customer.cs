namespace SupportHub.Customers.Api.Features.Customers;

public enum SupportTier
{
    Standard,
    Premium,
    Enterprise
}

public sealed class Customer
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public SupportTier SupportTier { get; set; }
}
