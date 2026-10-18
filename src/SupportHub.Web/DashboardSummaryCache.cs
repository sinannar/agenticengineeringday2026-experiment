using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace SupportHub.Web;

public sealed class DashboardSummaryCache(
    IDashboardSummaryCacheStore store,
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
            var value = await store.GetAsync(Key, cancellationToken);
            if (string.IsNullOrEmpty(value))
            {
                logger.LogDebug("Dashboard summary cache miss.");
                return null;
            }

            var entry = JsonSerializer.Deserialize<DashboardSummaryCacheEntry>(value, JsonOptions);
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
            await store.SetAsync(
                Key,
                JsonSerializer.Serialize(entry, JsonOptions),
                MaximumAge,
                cancellationToken);
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
            await store.DeleteAsync(Key, cancellationToken);
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

public interface IDashboardSummaryCacheStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    Task SetAsync(string key, string value, TimeSpan expiration, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed class RedisDashboardSummaryCacheStore(IConnectionMultiplexer redis) : IDashboardSummaryCacheStore
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await redis.GetDatabase().StringGetAsync(key).WaitAsync(cancellationToken);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan expiration,
        CancellationToken cancellationToken) =>
        await redis.GetDatabase().StringSetAsync(key, value, expiration).WaitAsync(cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        redis.GetDatabase().KeyDeleteAsync(key).WaitAsync(cancellationToken);
}
