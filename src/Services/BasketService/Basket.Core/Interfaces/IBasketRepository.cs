using Basket.Core.Entities;

namespace Basket.Core.Interfaces
{
    public interface IBasketRepository
    {
        Task<CustomerBasket?> GetBasketAsync(string customerId);
        Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket, TimeSpan? expiry = null);
        Task<bool> DeleteBasketAsync(string customerId);
    }
}
