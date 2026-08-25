using System.Text.Json;
using Basket.Core.Entities;
using Basket.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Basket.Infrastructure.Repositories
{
    /// <summary>
    /// 基於 Redis 的高併發購物車倉儲實作
    /// </summary>
    public class RedisBasketRepository : IBasketRepository
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisBasketRepository> _logger;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromDays(30);

        public RedisBasketRepository(IConnectionMultiplexer redis, ILogger<RedisBasketRepository> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        private static string GetKey(string customerId) => $"basket:{customerId}";

        public async Task<CustomerBasket?> GetBasketAsync(string customerId)
        {
            try
            {
                var data = await _database.StringGetAsync(GetKey(customerId));
                if (data.IsNullOrEmpty)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<CustomerBasket>(data.ToString()!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get basket for customer: {CustomerId}", customerId);
                return null;
            }
        }

        public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(basket);
                var created = await _database.StringSetAsync(
                    GetKey(basket.CustomerId),
                    json,
                    expiry ?? _defaultExpiry);

                if (!created)
                {
                    _logger.LogWarning("Problem occurred persisting basket for customer: {CustomerId}", basket.CustomerId);
                    return null;
                }

                _logger.LogInformation("Basket updated successfully for customer: {CustomerId}", basket.CustomerId);
                return await GetBasketAsync(basket.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update basket for customer: {CustomerId}", basket.CustomerId);
                return null;
            }
        }

        public async Task<bool> DeleteBasketAsync(string customerId)
        {
            try
            {
                return await _database.KeyDeleteAsync(GetKey(customerId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete basket for customer: {CustomerId}", customerId);
                return false;
            }
        }
    }
}
