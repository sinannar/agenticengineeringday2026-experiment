using System.Net;
using System.Net.Http;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Tests;

public class TicketValidationTests
{
    [Fact]
    public void ValidTicketRequest_IsAccepted()
    {
        var request = new TicketRequest(
            "Cannot sign in",
            "The customer cannot sign in to the portal.",
            TicketStatus.Open,
            TicketPriority.High,
            "Ada",
            Guid.NewGuid());

        Assert.Empty(TicketValidation.Validate(request));
    }

    [Fact]
    public void MissingRequiredFields_ReturnsStructuredErrors()
    {
        var request = new TicketRequest(
            null,
            null,
            (TicketStatus)99,
            (TicketPriority)99,
            null,
            Guid.Empty);

        var errors = TicketValidation.Validate(request);

        Assert.Contains("title", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("description", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("status", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("priority", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("customerId", errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void OverlongFields_AreRejectedAtContractBoundaries()
    {
        var request = new TicketRequest(
            new string('t', TicketValidation.TitleMaximumLength + 1),
            new string('d', TicketValidation.DescriptionMaximumLength + 1),
            TicketStatus.New,
            TicketPriority.Low,
            new string('a', TicketValidation.AssigneeMaximumLength + 1),
            Guid.NewGuid());

        var errors = TicketValidation.Validate(request);

        Assert.Contains("title", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("description", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("assignee", errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_LeavesCreatedAtAndUpdatesDomainFields()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        var request = new TicketRequest(
            " Updated title ",
            " Updated description ",
            TicketStatus.Resolved,
            TicketPriority.Medium,
            " Ada ",
            Guid.NewGuid());

        TicketValidation.Apply(ticket, request);

        Assert.Equal(createdAt, ticket.CreatedAt);
        Assert.Equal("Updated title", ticket.Title);
        Assert.Equal("Updated description", ticket.Description);
        Assert.Equal("Ada", ticket.Assignee);
        Assert.Equal(request.CustomerId, ticket.CustomerId);
    }
}

public class CustomerLookupClientTests
{
    [Fact]
    public async Task ExistsAsync_MapsTransportFailureToUnavailable()
    {
        using var httpClient = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("http://customers")
        };
        var client = new CustomerLookupClient(httpClient);

        var result = await client.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CustomerLookupResult.Unavailable, result);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Customer service unavailable.");
    }
}
