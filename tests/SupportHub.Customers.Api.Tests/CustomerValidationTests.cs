using SupportHub.Customers.Api.Features.Customers;

namespace SupportHub.Customers.Api.Tests;

public class CustomerValidationTests
{
    [Theory]
    [InlineData(SupportTier.Standard)]
    [InlineData(SupportTier.Premium)]
    [InlineData(SupportTier.Enterprise)]
    public void ValidCustomerRequest_IsAccepted(SupportTier supportTier)
    {
        var request = new CustomerRequest("Ada Lovelace", "ada@example.com", "+1 555 0100", supportTier);

        Assert.Empty(CustomerValidation.Validate(request));
    }

    [Fact]
    public void MissingRequiredFields_ReturnsStructuredErrors()
    {
        var request = new CustomerRequest(null, "not-an-email", null, (SupportTier)99);

        var errors = CustomerValidation.Validate(request);

        Assert.Contains("name", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("email", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("supportTier", errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void OverlongPhoneNumber_IsRejected()
    {
        var request = new CustomerRequest("Ada Lovelace", "ada@example.com", new string('1', CustomerValidation.PhoneMaximumLength + 1), SupportTier.Standard);

        var errors = CustomerValidation.Validate(request);

        Assert.Contains("phoneNumber", errors.Keys, StringComparer.OrdinalIgnoreCase);
    }
}
