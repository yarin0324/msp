using FluentResults;
using Order.Adapters.DTOs;
using Order.UseCase.Interfaces;
using OrderService.Domain.UseCase;

namespace Order.Adapters.Services
{
    public class OrderAdapterService : IOrderAdapterService
    {
        private readonly IGetOrderUseCase _getOrderUseCase;
        private readonly ICreateOrderUseCase _createOrderUseCase;

        public OrderAdapterService(IGetOrderUseCase getOrderUseCase, ICreateOrderUseCase createOrderUseCase)
        {
            this._getOrderUseCase = getOrderUseCase;
            this._createOrderUseCase = createOrderUseCase;
        }

        public async Task<Result<OrderDto>> GetOrderAsync(long id)
        {
            var order = await _getOrderUseCase.ExecuteAsync(id);

            return Result.Ok(Mappers.OrderMappers.ToDto(order));
        }

        public async Task<Result<OrderDto>> CreateOrderAsync(OrderCreationDto? creation)
        {
            if (creation == null || !creation.Amount.HasValue || string.IsNullOrWhiteSpace(creation.Currency))
            {
                return Result.Fail("Invalid order creation data: Amount and Currency are required.");
            }

            var order = await _createOrderUseCase.ExecuteAsync(creation.Amount.Value);

            return  Result.Ok(Mappers.OrderMappers.ToDto(order));
        }
    }
}
