using MassTransit;
using OrderService.Domain.UseCase;

namespace OrderService.Infrastructure.Messaging.Publishers
{
    public class OrderCreatedPublisher : IEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderCreatedPublisher(IPublishEndpoint publishEndpoint)
        {
            this._publishEndpoint = publishEndpoint;
        }

        public async Task PublishAsync<T>(T @event) where T : class
        {
            await _publishEndpoint.Publish(@event);
        }
    }
}
