using Consul;
using Serilog;

namespace Basket.API;

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
            endpoints.MapGet("/health", () => Results.Ok("Healthy")).AllowAnonymous();
        });

        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

        var registration = new AgentServiceRegistration
        {
            ID = $"basket-service-{Guid.NewGuid()}",
            Name = "Basket Service",
            Address = "host.docker.internal", // 配置為 Docker 環境
            Port = 5079,
            Check = new AgentServiceCheck
            {
                HTTP = "http://host.docker.internal:5079/health",
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