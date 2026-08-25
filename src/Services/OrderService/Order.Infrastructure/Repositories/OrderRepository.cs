using Dapper;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;
using OrderService.Infrastructure.Common;

namespace OrderService.Infrastructure.Repositories
{
    /// <summary>
    /// 訂單資料倉儲實作（採用 Dapper 進行高效能資料庫存取）
    /// 核心特色：整合 Transactional Outbox Pattern，在同一本機資料庫交易內原子寫入訂單與待派送事件。
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public OrderRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// 新增訂單並原子寫入 Outbox 事件酬載（保證雙寫一致性）
        /// </summary>
        public async Task<long> AddAsync(Order order, OutboxMessage? outboxMessage = null)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");
            using var transaction = dbConnection.BeginTransaction();

            try
            {
                // 1. 寫入主訂單記錄並取得自動產生之 OrderId
                const string addOrderCommand = @"INSERT INTO Orders (Amount, CustomerId, Currency, Status, CreateTime, UpdateTime) 
                                                 OUTPUT INSERTED.OrderId
                                                 VALUES (@Amount, @CustomerId, @Currency, @Status, @CreateTime, @UpdateTime)";

                var orderId = await dbConnection.QuerySingleAsync<long>(addOrderCommand, new
                {
                    order.Amount,
                    order.CustomerId,
                    order.Currency,
                    Status = (int)order.Status,
                    order.CreateTime,
                    order.UpdateTime
                }, transaction);

                // 2. 批次寫入訂單明細項目
                const string addOrderItemCommand = @"INSERT INTO OrderItems (OrderId, ProductId, Quantity) 
                                                     VALUES (@OrderId, @ProductId, @Quantity)";

                await dbConnection.ExecuteAsync(addOrderItemCommand, 
                    order.Items.Select(i => new { OrderId = orderId, i.ProductId, i.Quantity }), transaction);

                // 3. 在相同資料庫交易內寫入待派送事件至 Outbox 資料表（避免 Dual-Write 丟失訊息）
                if (outboxMessage != null)
                {
                    const string addOutboxCommand = @"INSERT INTO OutboxMessages (Id, OccurredOn, Type, Payload, ProcessedOn, Error)
                                                      VALUES (@Id, @OccurredOn, @Type, @Payload, @ProcessedOn, @Error)";

                    await dbConnection.ExecuteAsync(addOutboxCommand, new
                    {
                        outboxMessage.Id,
                        outboxMessage.OccurredOn,
                        outboxMessage.Type,
                        outboxMessage.Payload,
                        outboxMessage.ProcessedOn,
                        outboxMessage.Error
                    }, transaction);
                }

                // 4. 提交交易
                transaction.Commit();
                return orderId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 更新訂單狀態（如：Pending -> StockReserved -> Completed / Cancelled）
        /// </summary>
        public async Task<bool> UpdateStatusAsync(long orderId, OrderStatus status)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string updateCommand = @"UPDATE Orders 
                                           SET Status = @Status, UpdateTime = @UpdateTime 
                                           WHERE OrderId = @OrderId";

            var rowsAffected = await dbConnection.ExecuteAsync(updateCommand, new
            {
                OrderId = orderId,
                Status = (int)status,
                UpdateTime = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// 取得未處理之 Outbox 事件清單（供背景 Worker 輪詢派送）
        /// </summary>
        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedOutboxMessagesAsync(int batchSize = 20)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string command = @"SELECT TOP (@BatchSize) Id, OccurredOn, Type, Payload, ProcessedOn, Error 
                                     FROM OutboxMessages 
                                     WHERE ProcessedOn IS NULL 
                                     ORDER BY OccurredOn ASC";

            return await dbConnection.QueryAsync<OutboxMessage>(command, new { BatchSize = batchSize });
        }

        /// <summary>
        /// 將 Outbox 事件標記為已派送成功
        /// </summary>
        public async Task MarkOutboxMessageAsProcessedAsync(Guid messageId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string command = @"UPDATE OutboxMessages 
                                     SET ProcessedOn = @ProcessedOn, Error = NULL 
                                     WHERE Id = @Id";

            await dbConnection.ExecuteAsync(command, new { Id = messageId, ProcessedOn = DateTime.UtcNow });
        }

        /// <summary>
        /// 記錄 Outbox 事件派送失敗之錯誤訊息
        /// </summary>
        public async Task MarkOutboxMessageAsFailedAsync(Guid messageId, string error)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string command = @"UPDATE OutboxMessages 
                                     SET Error = @Error 
                                     WHERE Id = @Id";

            await dbConnection.ExecuteAsync(command, new { Id = messageId, Error = error });
        }

        /// <summary>
        /// 依據訂單識別碼查詢完整訂單（包含明細項目）
        /// </summary>
        public async Task<Order?> GetByIdAsync(long orderId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string getOrderCommand = @"SELECT TOP 1 OrderId, Amount, Currency, CustomerId, Status, CreateTime, UpdateTime 
                                             FROM Orders 
                                             WHERE OrderId = @OrderId";

            var order = await dbConnection.QuerySingleOrDefaultAsync<Order>(getOrderCommand, new { orderId });
            if (order == null)
            {
                return null;
            }

            const string getOrderItemCommand = @"SELECT OrderItemId, OrderId, ProductId, Quantity 
                                                 FROM OrderItems 
                                                 WHERE OrderId = @OrderId";

            var orderItems = await dbConnection.QueryAsync<OrderItem>(getOrderItemCommand, new { OrderId = orderId });
            order.Items = orderItems.ToList();

            return order;
        }
    }
}
