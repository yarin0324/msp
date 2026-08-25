using Common.Idempotency;
using Common.Messaging;
using Common.Observability;
using Common.Security;
using Consul;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// 1. 註冊 OpenTelemetry 全鏈路可觀測性
builder.Services.AddCustomObservability(builder.Configuration, "PaymentService");

// 2. 註冊介面冪等性防線 (Redis 分散式鎖 + 快取)
builder.Services.AddCustomIdempotency(builder.Configuration);

// 3. 註冊使用者上下文 (讀取 Gateway X-User-* 標頭)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

// 3. 註冊 Consul 客戶端
builder.Services.AddSingleton<IConsulClient, ConsulClient>(_ => new ConsulClient(cfg =>
{
    var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
    cfg.Address = new Uri(consulAddress);
}));

// 4. 註冊 MassTransit (發布 PaymentProcessedEvent)
var rabbitMqHost = builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672/";
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureStandardRabbitMqBus(context, rabbitMqHost);
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
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "PaymentService" })).AllowAnonymous();

app.Run();
