using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace RedisCacheDemo.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly CacheMetricsService _cacheMetricsService;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    //Per-key semaphores to prevent cache stampedes
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        CacheMetricsService cacheMetricsService,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _cacheMetricsService = cacheMetricsService;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _distributedCache.GetAsync(key, cancellationToken);

        return data == null
            ? default
            : JsonSerializer.Deserialize<T>(data, JsonSerializerOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        var data = JsonSerializer.Serialize(value, JsonSerializerOptions);
        await _distributedCache.SetStringAsync(key, data, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _distributedCache.RemoveAsync(key, cancellationToken);
    }
     
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());

        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await db.KeyDeleteAsync(key);
        }
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Fast path - cache hit, no locking needed
        var cachedData = await GetAsync<T>(key, cancellationToken);

        if (cachedData != null)
        {
            _logger.LogInformation("CACHE HIT - key:{Key}", key);
            _cacheMetricsService.RecordHit();
            return cachedData;
        }

        // Slow path - acquire a per-key semaphore
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Double-check if another thread may have populated while we waited
            cachedData = await GetAsync<T>(key, cancellationToken);
            if (cachedData != null)
            {
                _logger.LogInformation("CACHE HIT - key:{Key} (populated while waiting for lock)", key);
                _cacheMetricsService.RecordHit();
                return cachedData;
            }

            // Cache miss - call the factory to get the data
            _logger.LogInformation("CACHE MISS - key:{Key}", key);
            _cacheMetricsService.RecordMiss();

            var data = await factory(cancellationToken);

            if (data != null)
            {
                await SetAsync(key, data, expiration, cancellationToken);
            }

            return data;
        }
        finally
        {
            semaphore.Release();
            _locks.TryRemove(key, out _); // Clean up the semaphore to prevent memory leaks
        }
    }
}
