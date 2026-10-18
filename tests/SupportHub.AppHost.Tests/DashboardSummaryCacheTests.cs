using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using SupportHub.Web;

public sealed class DashboardSummaryCacheTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task SetThenGet_ReturnsCompleteSummaryWithThirtySecondExpiration()
    {
        var store = new FakeCacheStore();
        var cache = CreateCache(store);
        var summary = CreateSummary();

        await cache.SetAsync(summary, CancellationToken.None);
        var result = await cache.GetAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(summary.EvaluatedAtUtc, result!.EvaluatedAtUtc);
        Assert.Equal(summary.TotalTickets, result.TotalTickets);
        Assert.Equal(summary.OpenTickets, result.OpenTickets);
        Assert.Equal(summary.StatusBreakdown, result.StatusBreakdown);
        Assert.Equal(summary.PriorityBreakdown, result.PriorityBreakdown);
        Assert.Equal(summary.AtRiskSlaTickets, result.AtRiskSlaTickets);
        Assert.Equal(summary.BreachedSlaTickets, result.BreachedSlaTickets);
        Assert.Equal(summary.DataQuality, result.DataQuality);
        Assert.Equal(DashboardSummaryCache.Key, store.LastSetKey);
        Assert.Equal(DashboardSummaryCache.MaximumAge, store.LastExpiration);
    }

    [Fact]
    public async Task GetWithMetadata_ReturnsCacheAgeForFreshnessPresentation()
    {
        var store = new FakeCacheStore();
        var cache = CreateCache(store);
        var summary = CreateSummary();

        await cache.SetAsync(summary, CancellationToken.None);
        var result = await cache.GetWithMetadataAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(summary.EvaluatedAtUtc, result!.Summary.EvaluatedAtUtc);
        Assert.Equal(summary.TotalTickets, result.Summary.TotalTickets);
        Assert.Equal(summary.OpenTickets, result.Summary.OpenTickets);
        Assert.Equal(summary.StatusBreakdown, result.Summary.StatusBreakdown);
        Assert.Equal(summary.PriorityBreakdown, result.Summary.PriorityBreakdown);
        Assert.InRange(result.Age, TimeSpan.Zero, DashboardSummaryCache.MaximumAge);
    }

    [Fact]
    public async Task ExpiredEntry_IsDeletedAndTreatedAsMiss()
    {
        var store = new FakeCacheStore
        {
            Value = JsonSerializer.Serialize(new
            {
                storedAtUtc = DateTimeOffset.UtcNow - DashboardSummaryCache.MaximumAge - TimeSpan.FromSeconds(1),
                summary = CreateSummary()
            }, JsonOptions)
        };
        var cache = CreateCache(store);

        var result = await cache.GetAsync(CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(DashboardSummaryCache.Key, store.LastDeletedKey);
    }

    [Fact]
    public async Task BackendReadFailure_DegradesToCacheMiss()
    {
        var store = new FakeCacheStore
        {
            ReadException = new RedisException("Redis unavailable")
        };
        var cache = CreateCache(store);

        var result = await cache.GetAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task BackendWriteFailure_DoesNotBlockAuthoritativeCaller()
    {
        var store = new FakeCacheStore
        {
            WriteException = new RedisException("Redis unavailable")
        };
        var cache = CreateCache(store);

        await cache.SetAsync(CreateSummary(), CancellationToken.None);

        Assert.Null(store.Value);
    }

    [Fact]
    public async Task Invalidate_DeletesVersionedDashboardKey()
    {
        var store = new FakeCacheStore();
        var cache = CreateCache(store);

        await cache.InvalidateAsync(CancellationToken.None);

        Assert.Equal(DashboardSummaryCache.Key, store.LastDeletedKey);
    }

    private static DashboardSummaryCache CreateCache(FakeCacheStore store) =>
        new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<DashboardSummaryCache>.Instance);

    private static DashboardSummaryModel CreateSummary() =>
        new(
            DateTimeOffset.UtcNow,
            1,
            1,
            [new(TicketStatusModel.Open, 1)],
            [new(TicketPriorityModel.High, 1)],
            0,
            0,
            DashboardDataQualityModel.Complete);

    private sealed class FakeCacheStore : IDashboardSummaryCacheStore
    {
        public string? Value { get; set; }
        public Exception? ReadException { get; set; }
        public Exception? WriteException { get; set; }
        public string? LastSetKey { get; private set; }
        public TimeSpan LastExpiration { get; private set; }
        public string? LastDeletedKey { get; private set; }

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
        {
            if (ReadException is not null)
            {
                throw ReadException;
            }

            return Task.FromResult(Value);
        }

        public Task SetAsync(
            string key,
            string value,
            TimeSpan expiration,
            CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            LastSetKey = key;
            LastExpiration = expiration;
            Value = value;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            LastDeletedKey = key;
            Value = null;
            return Task.CompletedTask;
        }
    }
}
