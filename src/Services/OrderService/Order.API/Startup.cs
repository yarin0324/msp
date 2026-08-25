using Common.Idempotency;
using Common.Messaging;
using Common.Observability;
using Common.Security;
using Consul;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using OrderService.Application.Handlers;
using OrderService.Application.Validators;
using OrderService.Infrastructure.Messaging.Consumers;
using OrderService.Infrastructure.Messaging.Publishers;
using OrderService.WebApi.DependencyInjection;
using OrderService.WebApi.Middleware.Exception;
using OrderService.WebApi.Validators;
using Serilog;

namespace OrderService.WebApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // 註冊 OpenTelemetry 全鏈路可觀測性
        services.AddCustomObservability(Configuration, "OrderService");

        // 註冊介面冪等性服務 (Redis 分散式鎖 + 結果快取)
        services.AddCustomIdempotency(Configuration);

        // 註冊 HttpContext 存取器與目前使用者上下文 (自動讀取 Gateway 轉發之 X-User-* 標頭)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddSingleton<IConsulClient, ConsulClient>(
            _ => new ConsulClient(
                cfg => cfg.Address = new Uri("http://localhost:8500")));

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddLogging(logging =>
        {
            logging.AddConsole(); // 確保控制台日誌輸出
            logging.AddDebug();   // 支援除錯日誌
        });
        
        ServiceRegistration.AddApplicationServices(services);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetOrderQueryHandler).Assembly));
        
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateOrderRequestDtoValidator>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<InventoryDeductedConsumer>();
            x.AddConsumer<InventoryDeductedFailedConsumer>();
            x.AddConsumer<PaymentProcessedConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConn = Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672/";
                // 配置指數退避重試 (1s, 4s, 10s) 與 In-Memory Outbox
                cfg.ConfigureStandardRabbitMqBus(context, rabbitMqConn);
                cfg.ReceiveEndpoint("order-queue", e =>
                {
                    // 限流與 DLQ (Dead Letter Queue: order-queue_error)
                    e.ConfigureStandardEndpoint(context);
                });
            });
        });

        //services.AddMassTransit(x =>
        //{
        //    x.UsingRabbitMq((context, cfg) =>
        //    {
        //        cfg.Host("rabbitmq://localhost", h =>
        //        {
        //            h.Username("guest");
        //            h.Password("guest");
        //            h.Heartbeat(TimeSpan.FromSeconds(10));
        //            h.RequestedConnectionTimeout(TimeSpan.FromSeconds(10));
        //        });
        //        cfg.UseMessageRetry(r => r.Intervals(100, 200, 500, 1000));
        //    });
        //});

        // 配置 MassTransit
        //services.AddMassTransit(x =>
        //{
        //    // 註冊消費者
        //    x.AddConsumer<OrderCreatedConsumer>();

        //    // 配置 RabbitMQ
        //    x.UsingRabbitMq((context, cfg) =>
        //    {
        //        cfg.Host("rabbitmq://localhost", h =>
        //        {
        //            h.Username("guest");
        //            h.Password("guest");
        //            h.Heartbeat(TimeSpan.FromSeconds(10));
        //            h.RequestedConnectionTimeout(TimeSpan.FromSeconds(10));
        //        });

        //        // 配置消費者隊列
        //        cfg.ReceiveEndpoint("order-created-queue", e =>
        //        {
        //            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        //        });

        //        // 配置重試策略
        //        cfg.UseMessageRetry(r => r.Intervals(100, 200, 500, 1000));
        //    });
        //});

        // 註冊發布者與 Outbox 背景派送 Worker
        services.AddScoped<OrderCreatedPublisher>();
        services.AddHostedService<OrderService.Infrastructure.BackgroundServices.OutboxPublisherWorker>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();

        //app.UseSerilogRequestLogging(options =>
        //{
        //    // 如果要自訂訊息的範本格式，可以修改這裡，但修改後並不會影響結構化記錄的屬性
        //    options.MessageTemplate = "Handled {RequestPath}";

        //    // 預設輸出的紀錄等級為 Information，你可以在此修改記錄等級
        //    options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Information;

        //    // 你可以從 httpContext 取得 HttpContext 下所有可以取得的資訊！
        //    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        //    {
        //        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        //        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        //        diagnosticContext.Set("UserID", httpContext.User.Identity?.Name);
        //    };
        //});

        app.UseExceptionMiddleware();

        app.UseRouting();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/health", () => Results.Ok("Healthy")).AllowAnonymous();
        });

        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

        var registration = new AgentServiceRegistration
        {
            ID = $"order-service-{Guid.NewGuid()}",
            Name = "Order Service",
            Address = "host.docker.internal", // 配置為 Docker 環境
            Port = 5125,
            Check = new AgentServiceCheck
            {
                HTTP = "http://host.docker.internal:5125/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5), // 增加超時
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1) // 失敗後 1 分鐘移除服務
            }
        };

        lifetime.ApplicationStarted.Register(async () =>
        {
            await consulClient.Agent.ServiceRegister(registration);
            Console.WriteLine("Service Registered With Consul");
        });

        lifetime.ApplicationStopping.Register(async () =>
        {
            await consulClient.Agent.ServiceDeregister(registration.ID);
            Console.WriteLine("Service DeRegistered From Consul");
        });
    }
}