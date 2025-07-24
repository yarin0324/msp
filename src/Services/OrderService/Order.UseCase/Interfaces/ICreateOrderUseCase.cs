using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Domain.Common;

namespace OrderService.Application.Interfaces
{
    public interface ICreateOrderUseCase
    {
        Task<Result<OrderResponseDto>> ExecuteAsync(CreateOrderCommand command);
    }
}
