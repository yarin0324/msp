using InventoryService.Domain.Interfaces.Events;
using MassTransit;

namespace InventoryService.Infrastructure.Messaging.Publishers
{
    public class InventoryCreatedPublisher : IEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public InventoryCreatedPublisher(IPublishEndpoint publishEndpoint)
        {
            this._publishEndpoint = publishEndpoint;
        }

        public async Task PublishAsync<T>(T @event) where T : class
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            
            Console.WriteLine($"Publishing event: {@event.GetType().Name} at {DateTime.Now}");
            try
            {
                Console.WriteLine("Attempting to publish to RabbitMQ...");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await _publishEndpoint.Publish(@event, cts.Token);
                Console.WriteLine("Event published successfully");
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                Console.WriteLine($"BrokerUnreachableException: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw new InvalidOperationException("Cannot connect to RabbitMQ Server", ex);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine($"Operation canceled: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw new TimeoutException("Publishing to RabbitMQ timed out", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, Type: {ex.GetType().FullName}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        //public async Task PublishAsync<T>(T @event) where T : class
        //{
        //    try
        //    {
        //        await _publishEndpoint.Publish(@event);
        //    }
        //    catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        //    {
        //        throw new InvalidOperationException("Cannot connect to RabbitMQ Server", ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}
    }
}
