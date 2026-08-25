using System.Text.Json;
using Common.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Events;
using OrderService.Domain.Interfaces.Repositories;

namespace OrderService.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Outbox 背景派送 Worker (保證 At-least-once 訊息派送)
    /// </summary>
    public class OutboxPublisherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherWorker> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);

        public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisherWorker background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    var messages = await orderRepository.GetUnprocessedOutboxMessagesAsync(batchSize: 20);

                    foreach (var message in messages)
                    {
                        try
                        {
                            _logger.LogInformation("Publishing Outbox message Id: {Id}, Type: {Type}", message.Id, message.Type);

                            // 反序列化事件並派送到 RabbitMQ
                            if (message.Type.Contains(nameof(OrderCreatedEvent)))
                            {
                                var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);
                                if (orderCreatedEvent != null)
                                {
                                    await publishEndpoint.Publish<IOrderCreatedEvent>(orderCreatedEvent, stoppingToken);
                                }
                            }

                            await orderRepository.MarkOutboxMessageAsProcessedAsync(message.Id);
                            _logger.LogInformation("Successfully published Outbox message Id: {Id}", message.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to publish Outbox message Id: {Id}", message.Id);
                            await orderRepository.MarkOutboxMessageAsFailedAsync(message.Id, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in OutboxPublisherWorker execution cycle.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("OutboxPublisherWorker background service stopped.");
        }
    }
}
