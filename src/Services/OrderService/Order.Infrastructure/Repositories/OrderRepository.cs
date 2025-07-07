using System.Data;
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

        public async Task AddAsync(Order order)
        {
            await using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.OpenAsync();
            await using var transaction = await dbConnection.BeginTransactionAsync();

            try
            {
                var sqlCommand = @"INSERT INTO Orders (Amount, CustomerId, Currency, CreateTime) 
                               OUTPUT INSERTED.OrderId
                               VALUES (@Amount, @CustomerId, @Currency, @CreateTime)";

                var orderId = await dbConnection.QuerySingleAsync<long>(sqlCommand, order, transaction);

                order.GetType().GetProperty("OrderId")!.SetValue(order, orderId);

                sqlCommand = @"INSERT INTO OrderItems (OrderId, ProductId, Quantity) 
                           VALUES (@OrderId, @ProductId, @Quantity)";

                foreach (var item in order.Items)
                {
                    item.SetOrderId(orderId);
                    await dbConnection.ExecuteAsync(sqlCommand, order.Items, transaction);
                }

                await transaction.CommitAsync();
                order.SetOrderId(orderId);
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

            var sqlCommand = @"SELECT TOP 1 * FROM Orders WHERE OrderId = @OrderId";

            var order = await dbConnection.QuerySingleOrDefaultAsync<Order>(sqlCommand, new { orderId }, transaction);

            if (order == null)
            {
                return null;
            }

            sqlCommand = "SELECT * FROM OrderItems WHERE OrderId = @OrderId";

            var orderItems = await dbConnection.QueryAsync<OrderItem>(sqlCommand, new { OrderId = orderId }, transaction);

            order.Items = orderItems.ToList();

            return order;
        }
    }
}
