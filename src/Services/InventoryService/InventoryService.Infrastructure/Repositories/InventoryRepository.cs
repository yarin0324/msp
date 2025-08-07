using Dapper;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces.Repositories;
using InventoryService.Infrastructure.Common;

namespace InventoryService.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public InventoryRepository(IConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        public async Task UpdateAsync(Inventory inventory)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Inventory");
            
            using var transaction = dbConnection.BeginTransaction();

            try
            {
                throw new NotImplementedException();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<Inventory?> GetByProductIdAsync(string productId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Inventory");

            using var transaction = dbConnection.BeginTransaction();

            const string command = @"SELECT TOP 1 * FROM Inventory WHERE ProductId = @ProductId";

            var inventory = await dbConnection.QuerySingleOrDefaultAsync<Inventory>(command, new { ProductId = productId }, transaction);

            return inventory;
        }
    }
}
