using Common.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// 支付成功事件消費者：收到金流扣款完成事件後，將訂單狀態更新為 Completed (Saga 終態)
    /// </summary>
    public class PaymentProcessedConsumer : IConsumer<IPaymentProcessedEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<PaymentProcessedConsumer> _logger;

        public PaymentProcessedConsumer(IOrderRepository orderRepository, ILogger<PaymentProcessedConsumer> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPaymentProcessedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing PaymentProcessedEvent for OrderId: {OrderId}, PaymentId: {PaymentId}", 
                message.OrderId, message.PaymentId);

            var updated = await _orderRepository.UpdateStatusAsync(message.OrderId, OrderStatus.Completed);
            if (updated)
            {
                _logger.LogInformation("Order {OrderId} successfully marked as Completed.", message.OrderId);
            }
            else
            {
                _logger.LogWarning("Failed to update Order {OrderId} status to Completed.", message.OrderId);
            }
        }
    }
}
