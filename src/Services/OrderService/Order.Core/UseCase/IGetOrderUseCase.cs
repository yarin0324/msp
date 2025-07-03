using OrderService.Domain.Common;
using OrderService.Domain.Entities;

namespace OrderService.Domain.UseCase
{
    public interface IGetOrderUseCase
    {
        Task<Result<ProductOrder>> ExecuteAsync(long id);
    }
}
