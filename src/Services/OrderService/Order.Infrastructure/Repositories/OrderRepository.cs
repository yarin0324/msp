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

        public async Task<long> AddAsync(Order order)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");
            
            using var transaction = dbConnection.BeginTransaction();

            try
            {
                const string addOrderCommand = @"INSERT INTO Orders (Amount, CustomerId, Currency, CreateTime) 
                                                 OUTPUT INSERTED.OrderId
                                                 VALUES (@Amount, @CustomerId, @Currency, @CreateTime)";

                var orderId = await dbConnection.QuerySingleAsync<long>(addOrderCommand, order, transaction);

                const string addOrderItemCommand = @"INSERT INTO OrderItems (OrderId, ProductId, Quantity) 
                                                     VALUES (@OrderId, @ProductId, @Quantity)";

                await dbConnection.ExecuteAsync(addOrderItemCommand, order.Items.Select(i => new { OrderId = orderId, i.ProductId, i.Quantity }), transaction);

                transaction.Commit();

                return orderId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<Order?> GetByIdAsync(long orderId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Order");

            using var transaction = dbConnection.BeginTransaction();

            const string getOrderCommand = @"SELECT TOP 1 * FROM Orders WHERE OrderId = @OrderId";

            var order = await dbConnection.QuerySingleOrDefaultAsync<Order>(getOrderCommand, new { orderId }, transaction);

            if (order == null)
            {
                return null;
            }

            const string getOrderItemCommand = "SELECT * FROM OrderItems WHERE OrderId = @OrderId";

            var orderItems = await dbConnection.QueryAsync<OrderItem>(getOrderItemCommand, new { OrderId = orderId }, transaction);

            order.Items = orderItems.ToList();

            return order;
        }
    }
}
