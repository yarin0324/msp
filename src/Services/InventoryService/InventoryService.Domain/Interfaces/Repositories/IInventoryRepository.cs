using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces.Repositories
{
    public interface IInventoryRepository
    {
        Task<Inventory?> GetByProductIdAsync(string productId);
        
        Task UpdateAsync(Inventory inventory);
    }
}
