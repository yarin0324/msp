using Common.Contracts;
using MassTransit;

namespace OrderService.Infrastructure.Messaging.Consumers
{
    public class InventoryDeductedConsumer : IConsumer<IInventoryDeductedEvent>
    {
        public Task Consume(ConsumeContext<IInventoryDeductedEvent> context)
        {
            var message = context.Message;
            Console.WriteLine($"Inventory deducted: {message.ProductId}, Quantity: {message.Quantity}");
            return Task.CompletedTask;
        }
    }
}
