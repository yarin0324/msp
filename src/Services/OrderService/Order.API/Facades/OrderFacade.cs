using OrderService.Application.Interfaces;
using OrderService.Application.Queries;
using OrderService.Domain.Common;
using OrderService.WebApi.DTOs;
using OrderService.WebApi.Mappers;

namespace OrderService.WebApi.Facades
{
    public class OrderFacade
    {
        private readonly ICreateOrderUseCase _createOrderUseCase;
        private readonly IGetOrderUseCase _getOrderUseCase;

        public OrderFacade(ICreateOrderUseCase createOrderUseCase, IGetOrderUseCase getOrderUseCase)
        {
            _createOrderUseCase = createOrderUseCase;
            _getOrderUseCase = getOrderUseCase;
        }

        public async Task<Result<CreateOrderResponseDto>> CreateOrderAsync(CreateOrderRequestDto order)
        {
            var result = await _createOrderUseCase.ExecuteAsync(OrderMappers.ToCommandTest(order));

            return Result<CreateOrderResponseDto>.Success(OrderMappers.ToCreateOrderResponseDto(result.Value));
        }

        public async Task<Result<GetOrderResponseDto>> GetOrderAsync(long id)
        {
            var result = await _getOrderUseCase.ExecuteAsync(new GetOrderQuery { OrderId = id });
            return Result<GetOrderResponseDto>.Success(OrderMappers.ToGetOrderResponseDto(result.Value));
        }
    }
}
