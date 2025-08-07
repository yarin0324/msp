using Serilog;
using Winton.Extensions.Configuration.Consul;

namespace InventoryService.WebApi
{
    internal abstract class Program
    {
        public static int Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //資安: 避免橫幅抓取，移除 Server Header 資訊
            builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

            try
            {
                CreateHostBuilder(args).Build().Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "發生未預期錯誤...");

                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddConsul("Inventory/Config/Serilog", options =>
                    {
                        // Consul Server Address
                        options.ConsulConfigurationOptions = cfg =>
                        {
                            cfg.Address = new Uri("http://localhost:8500");
                        };
                        options.Optional = true; // 若 Consul 不可用，應用程式仍可啟動
                        options.ReloadOnChange = true; // 當 Consul 配置改變時自動重新載入
                        options.PollWaitTime = TimeSpan.FromSeconds(5); // 輪詢期間，避免過多請求
                        options.OnLoadException = exceptionContext => // 處理載入錯誤
                        {
                            Console.WriteLine($"Failed to load Consul config: {exceptionContext.Exception.Message}");
                            exceptionContext.Ignore = true; // 忽略錯誤，繼續執行
                        };
                    });

                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((hostingContext, services, loggerConfiguration) =>
                {
                    // 從 IConfiguration 讀取 Serilog 配置並初始化 Log.Logger
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                });
    }
}