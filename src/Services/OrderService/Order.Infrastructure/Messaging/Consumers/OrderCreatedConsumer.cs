using Common.Contracts;
using MassTransit;

namespace OrderService.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// 暫時放，未來應移至支付服務裡
    /// </summary>
    public class OrderCreatedConsumer : IConsumer<IOrderCreatedEvent>
    {
        public Task Consume(ConsumeContext<IOrderCreatedEvent> context)
        {
            var message = context.Message;
            Console.WriteLine($"OrderCreatedConsumer received OrderId: {message.OrderId}");
            return Task.CompletedTask;
        }
    }
}
