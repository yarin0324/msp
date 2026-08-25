using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Observability
{
    /// <summary>
    /// OpenTelemetry 全鏈路分散式可觀測性擴充方法
    /// 支援 W3C TraceContext 標準傳遞 (HTTP + RabbitMQ)
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        public static IServiceCollection AddCustomObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion = "1.0.0")
        {
            var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] 
                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
                ?? "http://localhost:4317";

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddTelemetrySdk();

            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation(opts =>
                        {
                            opts.RecordException = true;
                            // 排除健康檢查探針雜訊
                            opts.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
                        })
                        .AddHttpClientInstrumentation(opts =>
                        {
                            opts.RecordException = true;
                        })
                        .AddSource("MassTransit") // 自動捕獲 MassTransit 分散式事件鏈路
                        .AddSource(serviceName);

                    // 輸出至 OTLP 端點 (Jaeger / OTel Collector)
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(otlpEndpoint);
                        });
                    }

                    if (configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter"))
                    {
                        tracing.AddConsoleExporter();
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();

                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        metrics.AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(otlpEndpoint);
                        });
                    }
                });

            return services;
        }
    }
}
