using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace SupportHub.Web;

public sealed class DashboardSummaryCache(
    IConnectionMultiplexer redis,
    ILogger<DashboardSummaryCache> logger)
{
    public const string Key = "dashboard-summary:v1";
    public static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<DashboardSummaryModel?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var value = await redis
                .GetDatabase()
                .StringGetAsync(Key)
                .WaitAsync(cancellationToken);
            if (value.IsNullOrEmpty)
            {
                logger.LogDebug("Dashboard summary cache miss.");
                return null;
            }

            var entry = JsonSerializer.Deserialize<DashboardSummaryCacheEntry>(value.ToString(), JsonOptions);
            if (entry is null || DateTimeOffset.UtcNow - entry.StoredAtUtc > MaximumAge)
            {
                logger.LogDebug("Dashboard summary cache entry expired.");
                await InvalidateAsync(cancellationToken);
                return null;
            }

            logger.LogDebug("Dashboard summary cache hit with age {CacheAgeSeconds} seconds.",
                (DateTimeOffset.UtcNow - entry.StoredAtUtc).TotalSeconds);
            return entry.Summary;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Dashboard summary cache read failed; using the authoritative API.");
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Dashboard summary cache entry was invalid; using the authoritative API.");
            return null;
        }
    }

    public async Task SetAsync(DashboardSummaryModel summary, CancellationToken cancellationToken)
    {
        try
        {
            var entry = new DashboardSummaryCacheEntry(DateTimeOffset.UtcNow, summary);
            await redis
                .GetDatabase()
                .StringSetAsync(
                    Key,
                    JsonSerializer.Serialize(entry, JsonOptions),
                    MaximumAge)
                .WaitAsync(cancellationToken);
            logger.LogDebug("Dashboard summary cache entry stored.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Dashboard summary cache write failed.");
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(Key).WaitAsync(cancellationToken);
            logger.LogDebug("Dashboard summary cache entry invalidated.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Dashboard summary cache invalidation failed.");
        }
    }

    private sealed record DashboardSummaryCacheEntry(
        DateTimeOffset StoredAtUtc,
        DashboardSummaryModel Summary);
}
