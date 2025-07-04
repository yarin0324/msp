using OrderService.Domain.Common;
using OrderService.Domain.UseCase;
using OrderService.WebApi.DTOs;
using OrderService.WebApi.Mappers;

namespace OrderService.WebApi.Services
{
    public class OrderServiceFacade
    {
        private readonly ICreateOrderUseCase _createOrderUseCase;
        private readonly IGetOrderUseCase _getOrderUseCase;

        public OrderServiceFacade(ICreateOrderUseCase createOrderUseCase, IGetOrderUseCase getOrderUseCase)
        {
            _createOrderUseCase = createOrderUseCase;
            _getOrderUseCase = getOrderUseCase;
        }

        public async Task<Result<OrderDto>> CreateOrderAsync(OrderCreationDto order)
        {
            var result = await _createOrderUseCase.ExecuteAsync(order.Amount!.Value, order.Currency, order.CustomerId);
            return Result<OrderDto>.Success(OrderMappers.ToDto(result.Value));
        }

        public async Task<Result<OrderDto>> GetOrderAsync(long id)
        {
            var result = await _getOrderUseCase.ExecuteAsync(id);
            return Result<OrderDto>.Success(OrderMappers.ToDto(result.Value));
        }
    }
}
