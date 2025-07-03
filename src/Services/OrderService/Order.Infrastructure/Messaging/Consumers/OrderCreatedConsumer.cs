using MassTransit;
using OrderService.Domain.Events;

namespace OrderService.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// 暫時放，應該放在支付服務裡
    /// </summary>
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        public Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var @event = context.Message;

            // TODO: 支付服務處理邏輯
            Console.WriteLine();

            return Task.CompletedTask;
        }
    }
}
