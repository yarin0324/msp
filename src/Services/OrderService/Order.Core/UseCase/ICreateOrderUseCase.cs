using OrderService.Domain.Common;
using OrderService.Domain.Entities;

namespace OrderService.Domain.UseCase
{
    public interface ICreateOrderUseCase
    {
        Task<Result<ProductOrder>> ExecuteAsync(decimal amount, string currency, string customerId);
    }
}
