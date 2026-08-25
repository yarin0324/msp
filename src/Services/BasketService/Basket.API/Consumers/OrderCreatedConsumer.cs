using Basket.Core.Interfaces;
using Common.Contracts;
using MassTransit;

namespace Basket.API.Consumers
{
    /// <summary>
    /// 訂單建立事件消費者：在訂單成功建立後，自動清空該用戶的 Redis 購物車
    /// </summary>
    public class OrderCreatedConsumer : IConsumer<IOrderCreatedEvent>
    {
        private readonly IBasketRepository _basketRepository;
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(IBasketRepository basketRepository, ILogger<OrderCreatedConsumer> logger)
        {
            _basketRepository = basketRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Received OrderCreatedEvent for OrderId: {OrderId}, CustomerId: {CustomerId}. Clearing basket...", 
                message.OrderId, message.CustomerId);

            if (!string.IsNullOrEmpty(message.CustomerId))
            {
                var result = await _basketRepository.DeleteBasketAsync(message.CustomerId);
                if (result)
                {
                    _logger.LogInformation("Successfully cleared basket for CustomerId: {CustomerId}", message.CustomerId);
                }
                else
                {
                    _logger.LogInformation("No active basket found to clear for CustomerId: {CustomerId}", message.CustomerId);
                }
            }
        }
    }
}
