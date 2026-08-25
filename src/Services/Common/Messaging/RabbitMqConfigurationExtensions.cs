using MassTransit;

namespace Common.Messaging
{
    /// <summary>
    /// MassTransit RabbitMQ 企業級通訊佇列標準配置擴充方法
    /// 功能包含：指數退避重試、消費者流量管制（QoS Prefetch）、In-Memory Outbox 以及死信佇列（Dead Letter Queue）自動路由。
    /// </summary>
    public static class RabbitMqConfigurationExtensions
    {
        /// <summary>
        /// 配置標準 RabbitMQ Host 與全域重試策略
        /// </summary>
        public static void ConfigureStandardRabbitMqBus(
            this IRabbitMqBusFactoryConfigurator cfg,
            IBusRegistrationContext context,
            string rabbitMqConnectionString)
        {
            cfg.Host(rabbitMqConnectionString);

            // 1. 全域指數退避重試原則（初始 1 秒、最大 10 秒，最多重試 3 次）
            // 避免因短暫資料庫死結或網路抖動而直接中斷，同時防止頻繁重試壓垮資料庫
            cfg.UseMessageRetry(r =>
            {
                r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(10),
                    intervalDelta: TimeSpan.FromSeconds(3)
                );

                // 排除永久性業務例外狀況（此類錯誤重試無效，直接轉入死信佇列 DLQ）
                r.Ignore<ArgumentNullException>();
                r.Ignore<ArgumentException>();
            });

            // 2. 啟用消費者端記憶體 Outbox，確保消費過程中的事件發布與狀態處理具備原子性
            cfg.UseInMemoryOutbox(context);
        }

        /// <summary>
        /// 配置標準接收端點（Receive Endpoint），包含並行限流與死信佇列轉移
        /// </summary>
        public static void ConfigureStandardEndpoint(
            this IRabbitMqReceiveEndpointConfigurator endpoint,
            IBusRegistrationContext context,
            int prefetchCount = 16,
            int concurrentMessageLimit = 8)
        {
            // 流量管制（QoS）：設定訊息預取上限與最大並行處理數，避免突發大量訊息造成消費者記憶體崩潰
            endpoint.PrefetchCount = prefetchCount;
            endpoint.ConcurrentMessageLimit = concurrentMessageLimit;

            // MassTransit 預設已內建死信佇列路由機制：
            // 當重試次數耗盡時，自動將訊息本體連同例外堆疊追蹤（Stacktrace）路由至 {queue_name}_error 死信佇列
            // 未知或未被處理之訊息則轉移至 {queue_name}_skipped
            endpoint.ConfigureConsumers(context);
        }
    }
}
