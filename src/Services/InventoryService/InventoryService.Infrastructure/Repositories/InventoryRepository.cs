using Dapper;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces.Repositories;
using InventoryService.Infrastructure.Common;

namespace InventoryService.Infrastructure.Repositories
{
    /// <summary>
    /// 庫存資料倉儲實作
    /// 核心特色：運用資料庫列層級獨占鎖（Row-Level Exclusive Lock）進行單一原子更新，徹底防止並行超賣。
    /// </summary>
    public class InventoryRepository : IInventoryRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public InventoryRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// 原子條件扣減庫存
        /// 說明：只有在「當前真實庫存 >= 欲扣減數量」時才扣除，利用資料庫列鎖防止 Race Condition，受影響筆數大於 0 代表扣減成功。
        /// </summary>
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

        /// <summary>
        /// 更新庫存資訊
        /// </summary>
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

        /// <summary>
        /// 依商品識別碼查詢庫存狀態（純唯讀查詢，無交易鎖定與洩漏風險）
        /// </summary>
        public async Task<Inventory?> GetByProductIdAsync(string productId)
        {
            using var dbConnection = await _connectionFactory.CreateConnectionAsync("Inventory");

            const string command = @"SELECT TOP 1 ProductId, Quantity, CreateTime, UpdateTime 
                                     FROM Inventory 
                                     WHERE ProductId = @ProductId";

            var inventory = await dbConnection.QuerySingleOrDefaultAsync<Inventory>(command, new { ProductId = productId });
            return inventory;
        }
    }
}
