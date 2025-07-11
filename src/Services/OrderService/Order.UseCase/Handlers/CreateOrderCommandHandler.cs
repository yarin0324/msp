using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Events;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Events;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Application.Handlers
{
    /// <summary>
    /// 透過Mediator 實作 CQRS 的作法
    /// Mediator 類似 Facade，Web Api端可省去Facade，但專案規模大時，複雜度會上升
    /// </summary>
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderResponseDto>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEventPublisher _eventPublisher;

        public CreateOrderCommandHandler(IOrderRepository orderRepository, IEventPublisher eventPublisher)
        {
            this._orderRepository = orderRepository;
            this._eventPublisher = eventPublisher;
        }

        public async Task <Result<OrderResponseDto>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            var order = new Order(command.Amount, command.Currency, command.CustomerId,
                command.Items.Select(item => new OrderItem { ProductId = item.ProductId, Quantity = item.Quantity }).ToList());

            // 新增訂單資料
            var orderId = await _orderRepository.AddAsync(order);

            order.SetOrderId(orderId);

            foreach (var item in order.Items)
            {
                item.SetOrderId(orderId);
            }

            // 發佈創建訂單事件
            await _eventPublisher.PublishAsync(new OrderCreatedEvent
            {
                Id = order.OrderId,
                Amount = order.Amount,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                CreateTime = order.CreateTime,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            });

            return Result<OrderResponseDto>.Success(new OrderResponseDto
            {
                OrderId = order.OrderId
            });
        }
    }
}
