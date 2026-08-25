using Basket.API.Consumers;
using Basket.Core.Interfaces;
using Basket.Infrastructure.Repositories;
using Common.Messaging;
using Common.Observability;
using Common.Security;
using Consul;
using MassTransit;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 1. 註冊 OpenTelemetry 全鏈路可觀測性
builder.Services.AddCustomObservability(builder.Configuration, "BasketService");

// 2. 註冊 Redis 連線與倉儲
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<IBasketRepository, RedisBasketRepository>();

// 3. 註冊使用者上下文 (讀取 Gateway X-User-* 標頭)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

// 4. 註冊 Consul 客戶端
builder.Services.AddSingleton<IConsulClient, ConsulClient>(_ => new ConsulClient(cfg =>
{
    var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
    cfg.Address = new Uri(consulAddress);
}));

// 5. 註冊 MassTransit (消費 OrderCreatedEvent 自動清空購物車)
var rabbitMqHost = builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672/";
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureStandardRabbitMqBus(context, rabbitMqHost);
        cfg.ReceiveEndpoint("basket-order-created-queue", e =>
        {
            e.ConfigureStandardEndpoint(context);
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "BasketService" })).AllowAnonymous();

app.Run();