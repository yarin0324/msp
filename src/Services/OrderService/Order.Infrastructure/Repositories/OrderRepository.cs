using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string? _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OrderDb");
        }

        public async Task AddAsync(ProductOrder order)
        {
            using IDbConnection dbConnection = new SqlConnection(_connectionString);

            var sqlCommand = @"INSERT INTO Orders (Amount, CustomerId, Currency, CreateTime) 
                               OUTPUT INSERTED.Id 
                               VALUES (@Amount, @CustomerId, @Currency, @CreateTime)";

            var id = await dbConnection.QuerySingleAsync<long>(sqlCommand, order);

            order.GetType().GetProperty("Id")!.SetValue(order, id);
        }

        public async Task<ProductOrder?> GetByIdAsync(long id)
        {
            using IDbConnection dbConnection = new SqlConnection(_connectionString);

            var sqlCommand = @"SELECT TOP 1 * FROM Orders WHERE Id = @Id";

            var order = await dbConnection.QuerySingleOrDefaultAsync<ProductOrder>(sqlCommand, new { id });

            return order;
        }
    }
}
