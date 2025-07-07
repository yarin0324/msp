using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Events;
using OrderService.Application.Interfaces;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Events;
using OrderService.Domain.Interfaces.Repositories;
using OrderCreatedEvent = OrderService.Application.Events.OrderCreatedEvent;

namespace OrderService.Application.UseCases
{
    public class CreateOrderUseCase : ICreateOrderUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEventPublisher _eventPublisher;

        public CreateOrderUseCase(IOrderRepository orderRepository, IEventPublisher eventPublisher)
        {
            this._orderRepository = orderRepository;
            this._eventPublisher = eventPublisher;
        }

        public async Task<Result<OrderResponseDto>> ExecuteAsync(CreateOrderCommand command)
        {
            // TODO 可檢核Customer 存不存在、Currency是否正確等等

            var order = new Order(command.Amount, command.Currency, command.CustomerId, 
                command.Items.Select(item => new OrderItem { ProductId = item.ProductId, Quantity = item.Quantity}).ToList());

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
