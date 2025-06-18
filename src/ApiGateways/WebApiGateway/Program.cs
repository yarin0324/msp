using Microsoft.AspNetCore.Hosting.StaticWebAssets;

namespace WebApiGateway
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
                Console.WriteLine("網站開始啟動...");

                CreateHostBuilder(args).Build().Run();

                return 0;
            }
            catch (Exception ex)
            {
                //Log.Fatal(ex, "發生未預期錯誤...");

                return 1;
            }
            finally
            {
                //Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureAppConfiguration((ctx, cb) =>
                    {
                        if (!ctx.HostingEnvironment.IsDevelopment())
                        {
                            StaticWebAssetsLoader.UseStaticWebAssets(ctx.HostingEnvironment, ctx.Configuration);
                        }
                    });
                });
    }
}