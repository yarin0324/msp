using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Queries;
using OrderService.Domain.Common;

namespace OrderService.Application.Interfaces
{
    public interface IGetOrderUseCase
    {
        Task<Result<OrderResponseDto>> ExecuteAsync(GetOrderQuery query);
    }
}
