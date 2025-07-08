using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string? _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OrderDb");
        }

        public async Task<long> AddAsync(Order order)
        {
            await using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.OpenAsync();
            await using var transaction = await dbConnection.BeginTransactionAsync();

            try
            {
                const string addOrderCommand = @"INSERT INTO Orders (Amount, CustomerId, Currency, CreateTime) 
                                                 OUTPUT INSERTED.OrderId
                                                 VALUES (@Amount, @CustomerId, @Currency, @CreateTime)";

                var orderId = await dbConnection.QuerySingleAsync<long>(addOrderCommand, order, transaction);

                const string addOrderItemCommand = @"INSERT INTO OrderItems (OrderId, ProductId, Quantity) 
                                                     VALUES (@OrderId, @ProductId, @Quantity)";

                await dbConnection.ExecuteAsync(addOrderItemCommand, order.Items.Select(i => new { OrderId = orderId, i.ProductId, i.Quantity }), transaction);

                await transaction.CommitAsync();

                return orderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Order?> GetByIdAsync(long orderId)
        {
            await using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.OpenAsync();
            await using var transaction = await dbConnection.BeginTransactionAsync();

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
