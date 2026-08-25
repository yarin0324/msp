using Common.Contracts;
using FluentValidation;
using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Events;
using OrderService.Application.Interfaces;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Events;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Application.UseCases
{
    /// <summary>
    /// 傳統 UseCase 作法
    /// </summary>
    public class CreateOrderUseCase : ICreateOrderUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<CreateOrderCommand> _validator;

        public CreateOrderUseCase(IOrderRepository orderRepository, IEventPublisher eventPublisher, IValidator<CreateOrderCommand> validator)
        {
            this._validator = validator;
            this._orderRepository = orderRepository;
            this._eventPublisher = eventPublisher;
        }

        public async Task<Result<OrderResponseDto>> ExecuteAsync(CreateOrderCommand command)
        {
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
            await _eventPublisher.PublishAsync<IOrderCreatedEvent>(new OrderCreatedEvent
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
            });

            return Result<OrderResponseDto>.Success(new OrderResponseDto
            {
                OrderId = order.OrderId
            });
        }
    }
}
