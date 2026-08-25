using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Common.Resilience
{
    /// <summary>
    /// HTTP 彈性容錯與斷路器策略定義（基於 Polly 政策引擎）
    /// 用途：防範微服務間 HTTP 通訊因短暫網路抖動、下游服務當機所引發之級聯失效（Cascading Failure / 雪崩效應）。
    /// </summary>
    public static class HttpResiliencePolicies
    {
        /// <summary>
        /// 指數退避重試策略（3 次重試，附帶隨機抖動 Jitter）
        /// 抖動機制可避免大量並行請求同時重試引發伺服器驚群效應（Thundering Herd Problem）。
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger? logger = null)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning("HTTP 請求失敗，狀態碼: {StatusCode}。等待 {Timespan} 後執行第 #{Attempt} 次重試...",
                            outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message, timespan, retryAttempt);
                    });
        }

        /// <summary>
        /// 斷路器策略（Circuit Breaker）
        /// 規則：若連續發生 5 次非暫態錯誤，立即開啟斷路器 30 秒，期間直接阻斷新請求進行快速失敗（Fail-Fast）。
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger? logger = null)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, breakDelay) =>
                    {
                        logger?.LogCritical("斷路器已開啟 (OPEN)，熔斷時間: {Duration}，觸發原因: {Reason}", 
                            breakDelay, outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                    },
                    onReset: () =>
                    {
                        logger?.LogInformation("斷路器已重設為關閉狀態 (CLOSED)，下游服務通訊恢復正常。");
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogWarning("斷路器進入半開探針狀態 (HALF-OPEN)，正在發送探針請求測試下游服務健康狀態...");
                    });
        }

        /// <summary>
        /// 為 HttpClient 注入標準彈性容錯管線（重試 -> 斷路器 -> 超時逾時）
        /// </summary>
        public static IHttpClientBuilder AddDefaultResiliencePolicies(this IHttpClientBuilder builder)
        {
            return builder
                .AddPolicyHandler((sp, _) => GetRetryPolicy(sp.GetService<ILogger<HttpClient>>()))
                .AddPolicyHandler((sp, _) => GetCircuitBreakerPolicy(sp.GetService<ILogger<HttpClient>>()))
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));
        }
    }
}
