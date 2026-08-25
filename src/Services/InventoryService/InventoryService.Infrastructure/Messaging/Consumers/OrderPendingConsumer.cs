using Common.Contracts;
using InventoryService.Application.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Infrastructure.Messaging.Consumers
{
    public class OrderPendingConsumer : IConsumer<IOrderCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrderPendingConsumer> _logger;

        public OrderPendingConsumer(IMediator mediator, ILogger<OrderPendingConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing OrderCreatedEvent for OrderId: {OrderId} with {ItemCount} items", 
                message.OrderId, message.Items?.Count ?? 0);

            if (message.Items == null || !message.Items.Any())
            {
                _logger.LogWarning("OrderId {OrderId} has no items to deduct.", message.OrderId);
                return;
            }

            foreach (var item in message.Items)
            {
                var command = new DeductInventoryCommand
                {
                    OrderId = message.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                };

                var result = await _mediator.Send(command, context.CancellationToken);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to deduct inventory for OrderId: {OrderId}, ProductId: {ProductId}. Reason: {Reason}", 
                        message.OrderId, item.ProductId, result.Message);
                    // 扣減失敗時已在 Handler 內發布 InventoryDeductedFailedEvent 觸發 Saga 補償
                    break;
                }

                _logger.LogInformation("Successfully deducted inventory for OrderId: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}",
                    message.OrderId, item.ProductId, item.Quantity);
            }
        }
    }
}
