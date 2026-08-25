using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Common.Idempotency
{
    public static class IdempotencyExtensions
    {
        public static IServiceCollection AddCustomIdempotency(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            var redisConn = configuration.GetConnectionString("Redis") 
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis") 
                ?? "localhost:6379";

            // 若尚未註冊 IConnectionMultiplexer 則自動註冊
            services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.TryAddScoped<IIdempotencyStore, RedisIdempotencyStore>();

            return services;
        }
    }
}
