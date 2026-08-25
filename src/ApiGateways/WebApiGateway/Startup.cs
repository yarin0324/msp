using System.Text;
using Common.Observability;
using Common.Security;
using Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            // 1. 註冊 Consul Client
            services.AddSingleton<IConsulClient, ConsulClient>(_ => new ConsulClient(cfg =>
            {
                var address = Configuration["Consul:Address"] ?? "http://localhost:8500";
                cfg.Address = new Uri(address);
            }));

            // 2. 註冊 JWT Bearer 集中認證
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

            // 3. 註冊授權策略
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            // 4. 註冊 YARP 反向代理服務，掛載 Claims 轉發器與動態設定重載
            services.AddReverseProxy()
                .LoadFromConfig(Configuration.GetSection("ReverseProxy"))
                .AddTransforms<ClaimsTransformProvider>();

            // 5. 註冊 OpenTelemetry 全鏈路可觀測性
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

            // 啟用認證與授權中介軟體 (必須在 UseRouting 之後、UseEndpoints 之前)
            app.UseAuthentication();
            app.UseAuthorization();

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