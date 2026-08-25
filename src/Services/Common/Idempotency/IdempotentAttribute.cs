using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Idempotency
{
    /// <summary>
    /// 介面冪等性動作過濾器（Action Filter）
    /// 用途：防範前端連擊重複送出、網路瞬斷重試所引發之重複建立訂單或重複扣款。
    /// 支援請求標頭：Idempotency-Key 或 X-Idempotency-Key
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IdempotentAttribute : Attribute, IAsyncActionFilter
    {
        /// <summary>
        /// 是否強制要求客戶端必須附帶冪等性標頭
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// 執行成功後快取保留時間（小時）
        /// </summary>
        public int ExpiryHours { get; set; } = 24;

        /// <summary>
        /// 處理中狀態（In-Flight）之暫態鎖定存活時間（分鐘）
        /// </summary>
        public int InFlightLockMinutes { get; set; } = 2;

        public const string HeaderKey1 = "Idempotency-Key";
        public const string HeaderKey2 = "X-Idempotency-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var store = httpContext.RequestServices.GetRequiredService<IIdempotencyStore>();
            var logger = httpContext.RequestServices.GetService<ILogger<IdempotentAttribute>>();

            // 1. 解析 HTTP 請求標頭中的冪等性唯一識別碼
            string? idempotencyKey = httpContext.Request.Headers[HeaderKey1].FirstOrDefault()
                ?? httpContext.Request.Headers[HeaderKey2].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                if (Required)
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        message = $"標頭 '{HeaderKey1}' 為此冪等性操作之必填項目。"
                    });
                    return;
                }

                // 未帶 Key 且非強制要求時，直接放行進入下游管線
                await next();
                return;
            }

            // 2. 嘗試獲取 Redis 分散式暫態鎖定 (SETNX 原子操作)
            var lockAcquired = await store.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromMinutes(InFlightLockMinutes));

            if (!lockAcquired)
            {
                // 取得鎖定失敗：檢查是否存在先前已處理完成之快取回應
                var cachedResponse = await store.GetResponseAsync(idempotencyKey);
                if (cachedResponse != null)
                {
                    logger?.LogInformation("冪等性快取命中 (HIT)，鍵值: {Key}。直接重放快取回應，不重複執行業務邏輯。", idempotencyKey);
                    httpContext.Response.Headers["X-Cache-Lookup"] = "HIT-IDEMPOTENT";
                    
                    context.Result = new ContentResult
                    {
                        Content = cachedResponse.Body,
                        ContentType = cachedResponse.ContentType,
                        StatusCode = cachedResponse.StatusCode
                    };
                    return;
                }

                // 請求仍在處理中 (In-Flight)：回應 409 Conflict 告知用戶端避免重複並行送出
                logger?.LogWarning("偵測到並行重送之處理中請求 (In-Flight Conflict)，鍵值: {Key}", idempotencyKey);
                context.Result = new ObjectResult(new
                {
                    message = "具有相同 Idempotency-Key 之請求目前正在處理中，請稍候再試。"
                })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
                return;
            }

            // 3. 取得鎖定成功，放行執行控制器動作方法（Controller Action）
            ActionExecutedContext executedContext;
            try
            {
                executedContext = await next();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "執行冪等性動作方法時發生未處理之例外狀況，鍵值: {Key}。立即釋放鎖定以允許重試。", idempotencyKey);
                await store.ReleaseLockAsync(idempotencyKey);
                throw;
            }

            // 4. 若業務層拋出例外或回傳伺服器錯誤 (5xx)，釋放鎖定以允許後續重試
            if (executedContext.Exception != null || (executedContext.Result is ObjectResult objErr && objErr.StatusCode >= 500))
            {
                await store.ReleaseLockAsync(idempotencyKey);
                return;
            }

            // 5. 業務邏輯執行成功，將狀態碼、內容類型與 JSON 酬載快取至 Redis
            if (executedContext.Result is ObjectResult objResult)
            {
                var bodyText = JsonSerializer.Serialize(objResult.Value);
                var responseCache = new IdempotencyResponse
                {
                    StatusCode = objResult.StatusCode ?? 200,
                    ContentType = "application/json; charset=utf-8",
                    Body = bodyText,
                    CreatedAt = DateTime.UtcNow
                };

                await store.SaveResponseAsync(idempotencyKey, responseCache, TimeSpan.FromHours(ExpiryHours));
                httpContext.Response.Headers["X-Cache-Lookup"] = "MISS-IDEMPOTENT-SAVED";
            }
            else if (executedContext.Result is StatusCodeResult statusResult)
            {
                var responseCache = new IdempotencyResponse
                {
                    StatusCode = statusResult.StatusCode,
                    Body = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await store.SaveResponseAsync(idempotencyKey, responseCache, TimeSpan.FromHours(ExpiryHours));
                httpContext.Response.Headers["X-Cache-Lookup"] = "MISS-IDEMPOTENT-SAVED";
            }
        }
    }
}
