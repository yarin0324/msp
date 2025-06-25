using MassTransit;
using Order.Core.Interfaces;

namespace Order.Infrastructure.Messaging.Publishers
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
