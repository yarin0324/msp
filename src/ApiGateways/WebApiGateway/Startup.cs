using System.Text;
using System.Threading.RateLimiting;
using Common.Observability;
using Common.Security;
using Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using WebApiGateway.Transforms;

namespace WebApiGateway
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // 1. 註冊 Consul 客戶端
            services.AddSingleton<IConsulClient, ConsulClient>(_ => new ConsulClient(cfg =>
            {
                var address = Configuration["Consul:Address"] ?? "http://localhost:8500";
                cfg.Address = new Uri(address);
            }));

            // 2. 註冊 JWT Bearer 集中驗證
            var jwtOptions = Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
            var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // 3. 註冊授權原則
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            // 4. 註冊 ASP.NET Core 8 內建速率限制器 (Rate Limiter)
            services.AddRateLimiter(options =>
            {
                // 超過限流時自訂 429 Too Many Requests 回應
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"statusCode\":429,\"message\":\"請求過於頻繁，已觸發速率限制（Rate Limit），請稍候再試。\"}",
                        token);
                };

                // 原則 A: 一般查詢路由滑動窗口限流 (100 req / 60s)
                options.AddSlidingWindowLimiter("sliding-window-policy", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromSeconds(60);
                    opt.SegmentsPerWindow = 6;
                    opt.QueueLimit = 10;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // 原則 B: 敏感交易與身分驗證路由嚴格限流 (20 req / 60s)
                options.AddSlidingWindowLimiter("strict-auth-policy", opt =>
                {
                    opt.PermitLimit = 20;
                    opt.Window = TimeSpan.FromSeconds(60);
                    opt.SegmentsPerWindow = 6;
                    opt.QueueLimit = 5;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            });

            // 5. 註冊 YARP 反向代理服務，掛載 Claims 轉發器與動態設定重載
            services.AddReverseProxy()
                .LoadFromConfig(Configuration.GetSection("ReverseProxy"))
                .AddTransforms<ClaimsTransformProvider>();

            // 6. 註冊 OpenTelemetry 全鏈路可觀測性
            services.AddCustomObservability(Configuration, "WebApiGateway");

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            // 啟用認證與授權中介軟體
            app.UseAuthentication();
            app.UseAuthorization();

            // 啟用速率限制中介軟體 (Rate Limiter Middleware)
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy", gateway = "YARP 2.2.0" }));
                
                // 映射 YARP 反向代理管線
                endpoints.MapReverseProxy();
            });
        }
    }
}