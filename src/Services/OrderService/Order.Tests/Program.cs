using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Contracts;
using InventoryService.Application.Commands;
using InventoryService.Application.Events;
using InventoryService.Application.Handlers;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces.Events;
using InventoryService.Domain.Interfaces.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Events;
using OrderService.Application.Handlers;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Repositories;
using OrderService.Infrastructure.Messaging.Consumers;

namespace OrderService.Tests
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(">>> Running P0/P1 Architecture & Saga Unit Tests <<<");
            Console.WriteLine("==================================================");

            int passed = 0;
            int total = 0;

            async Task RunTest(string testName, Func<Task> testFunc)
            {
                total++;
                Console.Write($"[TEST {total}] {testName}... ");
                try
                {
                    await testFunc();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("PASSED");
                    Console.ResetColor();
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"FAILED: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }

            // Test 1: Order Entity Status Transitions
            await RunTest("Order Entity Initial Status and Status Transitions", () =>
            {
                var order = new Order(500, "USD", "CUST-001", new List<OrderItem>
                {
                    new OrderItem { ProductId = "PROD-A", Quantity = 2 }
                });

                if (order.Status != OrderStatus.Pending)
                    throw new Exception($"Expected initial status to be Pending, but got {order.Status}");

                order.SetStatus(OrderStatus.StockReserved);
                if (order.Status != OrderStatus.StockReserved)
                    throw new Exception($"Expected status to be StockReserved, but got {order.Status}");

                order.SetStatus(OrderStatus.Cancelled);
                if (order.Status != OrderStatus.Cancelled)
                    throw new Exception($"Expected status to be Cancelled, but got {order.Status}");

                return Task.CompletedTask;
            });

            // Test 2: InventoryDeductedConsumer (Saga Forward Step)
            await RunTest("InventoryDeductedConsumer updates order to StockReserved", async () =>
            {
                var fakeRepo = new FakeOrderRepository();
                var consumer = new InventoryDeductedConsumer(fakeRepo, NullLogger<InventoryDeductedConsumer>.Instance);

                var mockContext = new Mock<ConsumeContext<IInventoryDeductedEvent>>();
                mockContext.Setup(x => x.Message).Returns(new FakeInventoryDeductedEvent
                {
                    OrderId = 1001,
                    ProductId = "PROD-A",
                    Quantity = 2
                });

                await consumer.Consume(mockContext.Object);

                if (!fakeRepo.UpdatedStatuses.ContainsKey(1001) || fakeRepo.UpdatedStatuses[1001] != OrderStatus.StockReserved)
                {
                    throw new Exception("Order 1001 was not updated to StockReserved status.");
                }
            });

            // Test 3: InventoryDeductedFailedConsumer (Saga Compensating Step)
            await RunTest("InventoryDeductedFailedConsumer updates order to Cancelled", async () =>
            {
                var fakeRepo = new FakeOrderRepository();
                var consumer = new InventoryDeductedFailedConsumer(fakeRepo, NullLogger<InventoryDeductedFailedConsumer>.Instance);

                var mockContext = new Mock<ConsumeContext<IInventoryDeductedFailedEvent>>();
                mockContext.Setup(x => x.Message).Returns(new FakeInventoryDeductedFailedEvent
                {
                    OrderId = 2002,
                    ProductId = "PROD-OUT-OF-STOCK",
                    RequestedQuantity = 10,
                    Reason = "Out of stock"
                });

                await consumer.Consume(mockContext.Object);

                if (!fakeRepo.UpdatedStatuses.ContainsKey(2002) || fakeRepo.UpdatedStatuses[2002] != OrderStatus.Cancelled)
                {
                    throw new Exception("Order 2002 was not compensated and updated to Cancelled status.");
                }
            });

            // Test 4: DeductInventoryHandler Success Path
            await RunTest("DeductInventoryHandler with sufficient stock succeeds and publishes IInventoryDeductedEvent", async () =>
            {
                var fakeInvRepo = new FakeInventoryRepository { DeductResult = true };
                var fakePublisher = new FakeEventPublisher();
                var handler = new DeductInventoryHandler(fakeInvRepo, fakePublisher);

                var command = new DeductInventoryCommand
                {
                    OrderId = 3003,
                    ProductId = "PROD-B",
                    Quantity = 5
                };

                var result = await handler.Handle(command, CancellationToken.None);

                if (!result.IsSuccess || !result.Value)
                    throw new Exception("DeductInventoryHandler returned failure for sufficient stock.");

                if (fakePublisher.PublishedEvents.Count != 1)
                    throw new Exception($"Expected 1 published event, got {fakePublisher.PublishedEvents.Count}");

                if (fakePublisher.PublishedEvents[0] is not IInventoryDeductedEvent successEvent ||
                    successEvent.OrderId != 3003 || successEvent.ProductId != "PROD-B" || successEvent.Quantity != 5)
                {
                    throw new Exception("Published event did not match expected IInventoryDeductedEvent with OrderId 3003.");
                }
            });

            // Test 5: DeductInventoryHandler Failure Path
            await RunTest("DeductInventoryHandler with insufficient stock fails and publishes IInventoryDeductedFailedEvent", async () =>
            {
                var fakeInvRepo = new FakeInventoryRepository { DeductResult = false };
                var fakePublisher = new FakeEventPublisher();
                var handler = new DeductInventoryHandler(fakeInvRepo, fakePublisher);

                var command = new DeductInventoryCommand
                {
                    OrderId = 4004,
                    ProductId = "PROD-C",
                    Quantity = 999
                };

                var result = await handler.Handle(command, CancellationToken.None);

                if (result.IsSuccess)
                    throw new Exception("DeductInventoryHandler should have returned failure for insufficient stock.");

                if (fakePublisher.PublishedEvents.Count != 1)
                    throw new Exception($"Expected 1 published event, got {fakePublisher.PublishedEvents.Count}");

                if (fakePublisher.PublishedEvents[0] is not IInventoryDeductedFailedEvent failedEvent ||
                    failedEvent.OrderId != 4004 || failedEvent.ProductId != "PROD-C" || failedEvent.RequestedQuantity != 999)
                {
                    throw new Exception("Published event did not match expected IInventoryDeductedFailedEvent with OrderId 4004.");
                }
            });

            // Test 6: CreateOrderCommandHandler writes Outbox message atomically
            await RunTest("CreateOrderCommandHandler atomically creates Order and OutboxMessage", async () =>
            {
                var fakeRepo = new FakeOrderRepository();
                var handler = new CreateOrderCommandHandler(fakeRepo);

                var command = new CreateOrderCommand
                {
                    Amount = 1500,
                    Currency = "USD",
                    CustomerId = "CUST-OUTBOX-01",
                    Items = new List<OrderItemDto>
                    {
                        new OrderItemDto { ProductId = "PROD-100", Quantity = 2 }
                    }
                };

                var result = await handler.Handle(command, CancellationToken.None);

                if (!result.IsSuccess)
                    throw new Exception("CreateOrderCommandHandler failed.");

                if (fakeRepo.OutboxMessages.Count != 1)
                    throw new Exception($"Expected 1 Outbox message written to DB, got {fakeRepo.OutboxMessages.Count}");

                var outbox = fakeRepo.OutboxMessages[0];
                if (!outbox.Type.Contains(nameof(OrderCreatedEvent)))
                    throw new Exception($"Outbox type {outbox.Type} does not contain OrderCreatedEvent.");

                if (!outbox.Payload.Contains("CUST-OUTBOX-01"))
                    throw new Exception("Outbox payload does not contain serialized order information.");
            });

            Console.WriteLine("==================================================");
            Console.WriteLine($"Test Results: {passed}/{total} Passed");
            Console.WriteLine("==================================================");

            return passed == total ? 0 : 1;
        }
    }

    #region Fakes & Test Stubs

    public class FakeOrderRepository : IOrderRepository
    {
        public Dictionary<long, OrderStatus> UpdatedStatuses = new();
        public List<OutboxMessage> OutboxMessages = new();

        public Task<long> AddAsync(Order order, OutboxMessage? outboxMessage = null)
        {
            if (outboxMessage != null)
            {
                OutboxMessages.Add(outboxMessage);
            }
            return Task.FromResult(1L);
        }

        public Task<Order?> GetByIdAsync(long id) => Task.FromResult<Order?>(null);

        public Task<bool> UpdateStatusAsync(long orderId, OrderStatus status)
        {
            UpdatedStatuses[orderId] = status;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<OutboxMessage>> GetUnprocessedOutboxMessagesAsync(int batchSize = 20)
        {
            return Task.FromResult<IEnumerable<OutboxMessage>>(OutboxMessages.FindAll(m => m.ProcessedOn == null));
        }

        public Task MarkOutboxMessageAsProcessedAsync(Guid messageId)
        {
            var msg = OutboxMessages.Find(m => m.Id == messageId);
            msg?.MarkAsProcessed();
            return Task.CompletedTask;
        }

        public Task MarkOutboxMessageAsFailedAsync(Guid messageId, string error)
        {
            var msg = OutboxMessages.Find(m => m.Id == messageId);
            msg?.MarkAsFailed(error);
            return Task.CompletedTask;
        }
    }

    public class FakeInventoryRepository : IInventoryRepository
    {
        public bool DeductResult { get; set; } = true;

        public Task<bool> DeductStockAsync(string productId, int quantity, DateTime updateTime)
        {
            return Task.FromResult(DeductResult);
        }

        public Task<Inventory?> GetByProductIdAsync(string productId) => Task.FromResult<Inventory?>(null);
        public Task UpdateAsync(Inventory inventory) => Task.CompletedTask;
    }

    public class FakeEventPublisher : IEventPublisher
    {
        public List<object> PublishedEvents = new();

        public Task PublishAsync<T>(T @event) where T : class
        {
            PublishedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    public class FakeInventoryDeductedEvent : IInventoryDeductedEvent
    {
        public long OrderId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class FakeInventoryDeductedFailedEvent : IInventoryDeductedFailedEvent
    {
        public long OrderId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    #endregion
}
