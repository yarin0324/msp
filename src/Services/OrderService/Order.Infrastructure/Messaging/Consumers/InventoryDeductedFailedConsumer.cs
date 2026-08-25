using Common.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// 庫存扣減失敗消費者 (Saga 補償流程: 標記退單)
    /// </summary>
    public class InventoryDeductedFailedConsumer : IConsumer<IInventoryDeductedFailedEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<InventoryDeductedFailedConsumer> _logger;

        public InventoryDeductedFailedConsumer(IOrderRepository orderRepository, ILogger<InventoryDeductedFailedConsumer> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IInventoryDeductedFailedEvent> context)
        {
            var message = context.Message;
            _logger.LogWarning("Received InventoryDeductedFailedEvent for OrderId: {OrderId}, ProductId: {ProductId}, Reason: {Reason}",
                message.OrderId, message.ProductId, message.Reason);

            if (message.OrderId > 0)
            {
                var updated = await _orderRepository.UpdateStatusAsync(message.OrderId, OrderStatus.Cancelled);
                if (updated)
                {
                    _logger.LogInformation("Order {OrderId} status compensated and updated to Cancelled", message.OrderId);
                }
                else
                {
                    _logger.LogWarning("Order {OrderId} not found or status not updated during compensation", message.OrderId);
                }
            }
        }
    }
}
