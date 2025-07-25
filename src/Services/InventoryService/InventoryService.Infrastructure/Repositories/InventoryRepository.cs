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
            throw new NotImplementedException();
        }
    }
}
