using Dapper;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;
using OrderService.Infrastructure.Common;

namespace OrderService.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public OrderRepository(IConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        public async Task<long> AddAsync(Order order, OutboxMessage? outboxMessage = null)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");
            
            using var transaction = dbConnection.BeginTransaction();

            try
            {
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

                const string addOrderItemCommand = @"INSERT INTO OrderItems (OrderId, ProductId, Quantity) 
                                                     VALUES (@OrderId, @ProductId, @Quantity)";

                await dbConnection.ExecuteAsync(addOrderItemCommand, order.Items.Select(i => new { OrderId = orderId, i.ProductId, i.Quantity }), transaction);

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

                transaction.Commit();

                return orderId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

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

        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedOutboxMessagesAsync(int batchSize = 20)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string command = @"SELECT TOP (@BatchSize) Id, OccurredOn, Type, Payload, ProcessedOn, Error 
                                     FROM OutboxMessages 
                                     WHERE ProcessedOn IS NULL 
                                     ORDER BY OccurredOn ASC";

            return await dbConnection.QueryAsync<OutboxMessage>(command, new { BatchSize = batchSize });
        }

        public async Task MarkOutboxMessageAsProcessedAsync(Guid messageId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string command = @"UPDATE OutboxMessages 
                                     SET ProcessedOn = @ProcessedOn, Error = NULL 
                                     WHERE Id = @Id";

            await dbConnection.ExecuteAsync(command, new { Id = messageId, ProcessedOn = DateTime.UtcNow });
        }

        public async Task MarkOutboxMessageAsFailedAsync(Guid messageId, string error)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string command = @"UPDATE OutboxMessages 
                                     SET Error = @Error 
                                     WHERE Id = @Id";

            await dbConnection.ExecuteAsync(command, new { Id = messageId, Error = error });
        }

        public async Task<Order?> GetByIdAsync(long orderId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            const string getOrderCommand = @"SELECT TOP 1 OrderId, Amount, Currency, CustomerId, Status, CreateTime, UpdateTime FROM Orders WHERE OrderId = @OrderId";

            var order = await dbConnection.QuerySingleOrDefaultAsync<Order>(getOrderCommand, new { orderId });

            if (order == null)
            {
                return null;
            }

            const string getOrderItemCommand = "SELECT OrderItemId, OrderId, ProductId, Quantity FROM OrderItems WHERE OrderId = @OrderId";

            var orderItems = await dbConnection.QueryAsync<OrderItem>(getOrderItemCommand, new { OrderId = orderId });

            order.Items = orderItems.ToList();

            return order;
        }
    }
}
