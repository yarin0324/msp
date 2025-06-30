using FluentResults;
using Order.Adapters.DTOs;

namespace Order.Adapters.Interfaces
{
    public interface IOrderAdapterService
    {
        Task<Result<OrderDto>> CreateOrderAsync(OrderCreationDto? creation);
        Task<Result<OrderDto>> GetOrderAsync(long id);
    }
}
