using StackExchange.Redis;
using System.Text.Json;

namespace ProductAPIRedisCache.Infrastructure.Cache
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue
                    ? JsonSerializer.Deserialize<T>(value!)
                    : default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Redis Get Error (Key={key}), fallback to DB.");
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, json, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Redis Set Error (Key={key}), skip caching.");
                // No throw. Continue API logic.
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Redis Remove Error (Key={key}), skip removal.");
                // No throw. Continue API logic.
            }
        }

  
        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    // WARNING: .Keys() is SLOW and should not be used in prod for high QPS.
                    foreach (var key in server.Keys(pattern: pattern))
                    {
                        await _db.KeyDeleteAsync(key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Redis RemoveByPattern Error (Pattern={pattern}).");
            }
        }
    }
}
