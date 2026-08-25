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

        public async Task<bool> DeductStockAsync(string productId, int quantity, DateTime updateTime)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Inventory");

            const string deductCommand = @"
                UPDATE Inventory 
                SET Quantity = Quantity - @Quantity, 
                    UpdateTime = @UpdateTime 
                WHERE ProductId = @ProductId AND Quantity >= @Quantity";

            var rowsAffected = await dbConnection.ExecuteAsync(deductCommand, new 
            { 
                ProductId = productId, 
                Quantity = quantity, 
                UpdateTime = updateTime 
            });

            return rowsAffected > 0;
        }

        public async Task UpdateAsync(Inventory inventory)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Inventory");
            
            const string updateCommand = @"
                UPDATE Inventory 
                SET Quantity = @Quantity, 
                    UpdateTime = @UpdateTime 
                WHERE ProductId = @ProductId";

            await dbConnection.ExecuteAsync(updateCommand, new 
            { 
                ProductId = inventory.ProductId, 
                Quantity = inventory.Quantity, 
                UpdateTime = inventory.UpdateTime 
            });
        }

        public async Task<Inventory?> GetByProductIdAsync(string productId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Inventory");

            const string command = @"SELECT TOP 1 ProductId, Quantity, CreateTime, UpdateTime FROM Inventory WHERE ProductId = @ProductId";

            var inventory = await dbConnection.QuerySingleOrDefaultAsync<Inventory>(command, new { ProductId = productId });

            return inventory;
        }
    }
}
