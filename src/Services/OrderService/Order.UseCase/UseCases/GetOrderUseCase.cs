using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Queries;
using OrderService.Domain.Common;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Application.UseCases
{
    public class GetOrderUseCase : IGetOrderUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderUseCase(IOrderRepository orderRepository)
        {
            this._orderRepository = orderRepository;
        }

        public async Task<Result<OrderResponseDto>> ExecuteAsync(GetOrderQuery query)
        {
            var order = await _orderRepository.GetByIdAsync(query.OrderId);

            if (order == null)
            {
                throw new Exception($"Order with ID {query.OrderId} does not exist.");
            }

            return Result<OrderResponseDto>.Success(
                new OrderResponseDto
                {
                    OrderId = order.OrderId,
                    Amount = order.Amount,
                    CustomerId = order.CustomerId,
                    Currency = order.Currency,
                    CreateTime = order.CreateTime,
                    Items = order.Items.Select(i => new OrderItemDto()
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList()
                });
        }
    }
}
