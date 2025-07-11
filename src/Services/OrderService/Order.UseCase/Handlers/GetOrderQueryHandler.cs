using MediatR;
using OrderService.Application.Dtos;
using OrderService.Application.Queries;
using OrderService.Domain.Common;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Application.Handlers
{
    public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<OrderResponseDto>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderQueryHandler(IOrderRepository orderRepository)
        {
            this._orderRepository = orderRepository;
        }

        public async Task<Result<OrderResponseDto>> Handle(GetOrderQuery query, CancellationToken cancellationToken)
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
