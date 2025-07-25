using InventoryService.Application.Events;
using MassTransit;

namespace InventoryService.Infrastructure.Messaging.Consumers
{
    public class OrderPendingConsumer : IConsumer<OrderPendingEvent>
    {
        public Task Consume(ConsumeContext<OrderPendingEvent> context)
        {
            var @event = context.Message;

            Console.WriteLine();

            return Task.CompletedTask;
        }
    }
}
