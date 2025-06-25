using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Order.Core.Entities;
using Order.Core.Events;
using Order.Core.Interfaces;

namespace Order.Core.UseCase
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

        public async Task<ProductOrder> ExecuteAsync(decimal amount)
        {
            var order = new ProductOrder(amount);

            // 新增訂單資料
            await _orderRepository.AddAsync(order);

            // 發佈創建訂單事件
            await _eventPublisher.PublishAsync(new OrderCreatedEvent
            {
                Id = order.Id,
                Amount = order.Amount,
                CreateTime = order.CreateTime
            });

            return order;
        }
    }
}
