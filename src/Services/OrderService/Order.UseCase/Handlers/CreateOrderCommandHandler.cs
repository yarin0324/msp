using System.Text.Json;
using Common.Contracts;
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
    /// 透過 Mediator 實作 CQRS + Transactional Outbox Pattern
    /// </summary>
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderResponseDto>>
    {
        private readonly IOrderRepository _orderRepository;

        public CreateOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<OrderResponseDto>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            var order = new Order(command.Amount, command.Currency, command.CustomerId,
                command.Items.Select(item => new OrderItem { ProductId = item.ProductId, Quantity = item.Quantity }).ToList());

            // 準備事件實體
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.OrderId,
                Amount = order.Amount,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                CreateTime = order.CreateTime,
                Items = order.Items.Select(i => (IOrderItemContract)new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            // 序列化為 Outbox 訊息
            var outboxMessage = new OutboxMessage(
                typeof(OrderCreatedEvent).AssemblyQualifiedName ?? nameof(OrderCreatedEvent),
                JsonSerializer.Serialize(orderCreatedEvent)
            );

            // 在同一個 DB Transaction 內寫入訂單與 Outbox 訊息 (保證雙寫一致性)
            var orderId = await _orderRepository.AddAsync(order, outboxMessage);
            order.SetOrderId(orderId);

            return Result<OrderResponseDto>.Success(new OrderResponseDto
            {
                OrderId = order.OrderId,
                Amount = order.Amount,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                Status = order.Status.ToString(),
                CreateTime = order.CreateTime
            });
        }
    }
}
