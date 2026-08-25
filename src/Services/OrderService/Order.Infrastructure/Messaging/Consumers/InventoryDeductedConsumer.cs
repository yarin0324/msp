using Common.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Infrastructure.Messaging.Consumers
{
    public class InventoryDeductedConsumer : IConsumer<IInventoryDeductedEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<InventoryDeductedConsumer> _logger;

        public InventoryDeductedConsumer(IOrderRepository orderRepository, ILogger<InventoryDeductedConsumer> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IInventoryDeductedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Received InventoryDeductedEvent for OrderId: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}",
                message.OrderId, message.ProductId, message.Quantity);

            if (message.OrderId > 0)
            {
                var updated = await _orderRepository.UpdateStatusAsync(message.OrderId, OrderStatus.StockReserved);
                if (updated)
                {
                    _logger.LogInformation("Order {OrderId} status updated to StockReserved", message.OrderId);
                }
                else
                {
                    _logger.LogWarning("Order {OrderId} not found or status not updated", message.OrderId);
                }
            }
        }
    }
}
