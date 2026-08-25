using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        /// <summary>
        /// 查詢訂單: 透過訂單ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Order?> GetByIdAsync(long id);

        /// <summary>
        /// 新增訂單 (可選擇性附帶 OutboxMessage 在同一本機交易內原子寫入)
        /// </summary>
        /// <param name="order"></param>
        /// <param name="outboxMessage"></param>
        /// <returns></returns>
        Task<long> AddAsync(Order order, OutboxMessage? outboxMessage = null);

        /// <summary>
        /// 更新訂單狀態
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<bool> UpdateStatusAsync(long orderId, OrderStatus status);

        /// <summary>
        /// 取得未派送的 Outbox 訊息批次
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        Task<IEnumerable<OutboxMessage>> GetUnprocessedOutboxMessagesAsync(int batchSize = 20);

        /// <summary>
        /// 標記 Outbox 訊息已成功派送
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Task MarkOutboxMessageAsProcessedAsync(Guid messageId);

        /// <summary>
        /// 標記 Outbox 訊息派送失敗
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        Task MarkOutboxMessageAsFailedAsync(Guid messageId, string error);
    }
}
