using Common.Contracts;
using Consul;
using FluentValidation;
using FluentValidation.AspNetCore;
using InventoryService.Application.Commands;
using InventoryService.Application.Events;
using InventoryService.Application.Handlers;
using InventoryService.Application.Middleware.Exception;
using InventoryService.Application.Queries;
using InventoryService.Application.Validators;
using InventoryService.Infrastructure.Messaging.Publishers;
using InventoryService.WebApi.DependencyInjection;
using MassTransit;
using Serilog;

namespace InventoryService.WebApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
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
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CheckInventoryHandler).Assembly));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DeductInventoryHandler).Assembly));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetInventoryHandler).Assembly));
        
        services.AddMassTransit(x =>
        {   
            x.AddRequestClient<IInventoryDeductedEvent>();
            x.UsingRabbitMq((context, cfg) =>
            {
                //cfg.Host("amqp://guest:guest@localhost:5672/");
                cfg.Host(Configuration.GetConnectionString("RabbitMQ"));
                cfg.ReceiveEndpoint("inventory-queue", e =>
                {
                    e.ConfigureConsumers(context);
                    //e.ConfigureConsumer<MonitoringConsumer>(context);
                });
            });
        });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<DeductInventoryCommandValidator>();

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

        // 註冊發布者
        services.AddScoped<InventoryCreatedPublisher>();
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

        app.UseRouting();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            //endpoints.MapGet("/health", () => Results.Ok("Healthy")).AllowAnonymous();
        });

        app.UseExceptionMiddleware();

        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

        var registration = new AgentServiceRegistration
        {
            ID = $"inventory-service-{Guid.NewGuid()}",
            Name = "Inventory Service",
            Address = "host.docker.internal", // 配置為 Docker 環境
            Port = 5271,
            Check = new AgentServiceCheck
            {
                HTTP = "http://host.docker.internal:5271/health",
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