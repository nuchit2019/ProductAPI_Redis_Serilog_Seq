using StackExchange.Redis;
using System.Text.Json;

namespace ProductAPIRedisCache.Infrastructure.Cache
{
    public class RedisCacheService(IConnectionMultiplexer redis) : IRedisCacheService
    {
        private readonly IDatabase db = redis.GetDatabase();

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await db.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await db.KeyDeleteAsync(key);
        }

        
        public async Task RemoveByPatternAsync(string pattern)
        {
            
            var endpoints = redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: pattern))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
        }


    }
}
