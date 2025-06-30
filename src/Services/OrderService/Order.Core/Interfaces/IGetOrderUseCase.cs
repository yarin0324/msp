using Order.Core.Entities;

namespace Order.Core.Interfaces
{
    public interface IGetOrderUseCase
    {
        Task<ProductOrder> ExecuteAsync(long id);
    }
}
