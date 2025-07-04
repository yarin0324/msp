using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.Repositories;
using OrderService.Domain.UseCase;

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

        public async Task<Result<ProductOrder>> ExecuteAsync(decimal amount, string currency, string customerId)
        {
            // TODO 可檢核Customer 存不存在、Currency是否正確等等

            var order = new ProductOrder(amount, currency, customerId);

            // 新增訂單資料
            await _orderRepository.AddAsync(order);

            // 發佈創建訂單事件
            await _eventPublisher.PublishAsync(new OrderCreatedEvent
            {
                Id = order.Id,
                Amount = order.Amount,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                CreateTime = order.CreateTime
            });

            return Result<ProductOrder>.Success(order);
        }
    }
}
