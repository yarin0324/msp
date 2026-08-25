using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Idempotency
{
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisIdempotencyStore> _logger;
        private const string InFlightValue = "__IN_FLIGHT__";

        public RedisIdempotencyStore(IConnectionMultiplexer redis, ILogger<RedisIdempotencyStore> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        private static string FormatKey(string key) => $"idempotency:{key}";

        public async Task<bool> TryAcquireLockAsync(string key, TimeSpan lockExpiry)
        {
            try
            {
                var redisKey = FormatKey(key);
                // 使用 Redis 原子 SETNX (SetIfNotExists) 操作上鎖
                return await _database.StringSetAsync(redisKey, InFlightValue, lockExpiry, When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring idempotency lock for key: {Key}", key);
                return false;
            }
        }

        public async Task<IdempotencyResponse?> GetResponseAsync(string key)
        {
            try
            {
                var redisKey = FormatKey(key);
                var value = await _database.StringGetAsync(redisKey);
                if (value.IsNullOrEmpty || value == InFlightValue)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<IdempotencyResponse>(value.ToString()!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading idempotency response for key: {Key}", key);
                return null;
            }
        }

        public async Task SaveResponseAsync(string key, IdempotencyResponse response, TimeSpan dataExpiry)
        {
            try
            {
                var redisKey = FormatKey(key);
                var json = JsonSerializer.Serialize(response);
                await _database.StringSetAsync(redisKey, json, dataExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving idempotency response for key: {Key}", key);
            }
        }

        public async Task ReleaseLockAsync(string key)
        {
            try
            {
                var redisKey = FormatKey(key);
                var value = await _database.StringGetAsync(redisKey);
                if (value == InFlightValue)
                {
                    await _database.KeyDeleteAsync(redisKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing idempotency lock for key: {Key}", key);
            }
        }
    }
}
