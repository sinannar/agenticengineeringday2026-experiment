namespace SupportHub.Customers.Api.Features.Customers;

public sealed record CustomerTierBatchRequest(IReadOnlyList<Guid>? CustomerIds);

public sealed record CustomerTierResponse(Guid CustomerId, SupportTier SupportTier);
