using FluentValidation;
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
    /// <summary>
    /// 傳統UseCase作法
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
            // TODO 可檢核Customer 存不存在、Currency是否正確等等

            // FluentValidation 官方不能自動驗證了，要用FluentValidation.AspNetCore，但FluentValidation.AspNetCore不更新了
            //var vs = await _validator.ValidateAsync(command); 

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
