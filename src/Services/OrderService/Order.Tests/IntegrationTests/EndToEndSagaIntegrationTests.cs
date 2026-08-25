using System.Text.Json;
using Basket.Core.Entities;
using Basket.Infrastructure.Repositories;
using Common.Contracts;
using Common.Idempotency;
using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Messaging.Consumers;
using OrderService.Infrastructure.Repositories;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

// 定義別名以消除命名空間衝突
using OrderEntity = OrderService.Domain.Entities.Order;
using BasketOrderCreatedConsumer = Basket.API.Consumers.OrderCreatedConsumer;
using OrderConnectionFactory = OrderService.Infrastructure.Common.IConnectionFactory;
using InventoryConnectionFactory = InventoryService.Infrastructure.Common.IConnectionFactory;

namespace OrderService.Tests.IntegrationTests
{
    /// <summary>
    /// 基於 Testcontainers 之端到端真實容器整合測試
    /// 在無須預先架設環境的情況下，自動啟動真實 SQL Server, RabbitMQ, Redis 臨時容器執行完整微服務交易驗證。
    /// </summary>
    public class EndToEndSagaIntegrationTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();

        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        private readonly RedisContainer _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        public async Task InitializeAsync()
        {
            // 並行啟動三個真實基礎建設容器
            await Task.WhenAll(
                _msSqlContainer.StartAsync(),
                _rabbitMqContainer.StartAsync(),
                _redisContainer.StartAsync()
            );

            // 初始化 SQL Server 綱要與資料表
            using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
            await connection.OpenAsync();

            const string createTablesSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
                CREATE TABLE Orders (
                    OrderId BIGINT IDENTITY(1,1) PRIMARY KEY,
                    Amount DECIMAL(18,2) NOT NULL,
                    CustomerId NVARCHAR(100) NOT NULL,
                    Currency NVARCHAR(10) NOT NULL,
                    Status INT NOT NULL,
                    CreateTime DATETIME2 NOT NULL,
                    UpdateTime DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
                CREATE TABLE OrderItems (
                    OrderItemId BIGINT IDENTITY(1,1) PRIMARY KEY,
                    OrderId BIGINT NOT NULL,
                    ProductId NVARCHAR(50) NOT NULL,
                    Quantity INT NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OutboxMessages' AND xtype='U')
                CREATE TABLE OutboxMessages (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    OccurredOn DATETIME2 NOT NULL,
                    Type NVARCHAR(200) NOT NULL,
                    Payload NVARCHAR(MAX) NOT NULL,
                    ProcessedOn DATETIME2 NULL,
                    Error NVARCHAR(MAX) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Inventory' AND xtype='U')
                CREATE TABLE Inventory (
                    ProductId NVARCHAR(50) PRIMARY KEY,
                    Quantity INT NOT NULL,
                    CreateTime DATETIME2 NOT NULL,
                    UpdateTime DATETIME2 NOT NULL
                );

                DELETE FROM Inventory;
                INSERT INTO Inventory (ProductId, Quantity, CreateTime, UpdateTime)
                VALUES ('PROD-TEST-A', 10, GETUTCDATE(), GETUTCDATE()),
                       ('PROD-TEST-B', 1, GETUTCDATE(), GETUTCDATE());
            ";

            await connection.ExecuteAsync(createTablesSql);
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(
                _msSqlContainer.DisposeAsync().AsTask(),
                _rabbitMqContainer.DisposeAsync().AsTask(),
                _redisContainer.DisposeAsync().AsTask()
            );
        }

        [Fact(DisplayName = "1. 高並行庫存扣減測試：資料庫列鎖應徹底防止超賣")]
        public async Task Test_1_AtomicInventoryDeduction_ShouldPreventOverselling()
        {
            var mockFactory = new Mock<InventoryConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<string>()))
                .ReturnsAsync(() => new SqlConnection(_msSqlContainer.GetConnectionString()));

            var inventoryRepo = new InventoryService.Infrastructure.Repositories.InventoryRepository(mockFactory.Object);

            // 初始庫存為 1 件，模擬 2 個並行請求同時搶購 1 件商品
            var task1 = inventoryRepo.DeductStockAsync("PROD-TEST-B", 1, DateTime.UtcNow);
            var task2 = inventoryRepo.DeductStockAsync("PROD-TEST-B", 1, DateTime.UtcNow);

            var results = await Task.WhenAll(task1, task2);

            // 必須恰好一人成功 (true)，一人失敗 (false)，絕對不超賣
            Assert.Contains(true, results);
            Assert.Contains(false, results);

            var finalStock = await inventoryRepo.GetByProductIdAsync("PROD-TEST-B");
            Assert.NotNull(finalStock);
            Assert.Equal(0, finalStock.Quantity);
        }

        [Fact(DisplayName = "2. 完整 Saga 交易閉環測試：建立訂單 -> 鎖定庫存 -> 支付完成")]
        public async Task Test_2_EndToEndSaga_OrderCreated_StockReserved_PaymentCompleted()
        {
            var mockFactory = new Mock<OrderConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<string>()))
                .ReturnsAsync(() => new SqlConnection(_msSqlContainer.GetConnectionString()));

            var orderRepo = new OrderRepository(mockFactory.Object);

            // 步驟 A: 建立訂單與 Outbox 訊息 (原子交易寫入)
            var order = new OrderEntity(
                amount: 50000,
                currency: "TWD",
                customerId: "USR-INTEGRATION-001",
                items: new List<OrderItem>
                {
                    new OrderItem("PROD-TEST-A", 2)
                }
            );

            var outboxMessage = new OutboxMessage(
                "OrderCreatedEvent", 
                JsonSerializer.Serialize(new { OrderId = 1, CustomerId = order.CustomerId }));

            var orderId = await orderRepo.AddAsync(order, outboxMessage);
            Assert.True(orderId > 0);

            // 步驟 B: 模擬收到庫存扣減成功事件 (Saga 推進至 StockReserved)
            var deductedConsumer = new InventoryDeductedConsumer(orderRepo, NullLogger<InventoryDeductedConsumer>.Instance);
            var mockDeductedCtx = new Mock<ConsumeContext<IInventoryDeductedEvent>>();
            mockDeductedCtx.Setup(c => c.Message.OrderId).Returns(orderId);
            mockDeductedCtx.Setup(c => c.Message.ProductId).Returns("PROD-TEST-A");
            mockDeductedCtx.Setup(c => c.Message.Quantity).Returns(2);

            await deductedConsumer.Consume(mockDeductedCtx.Object);

            var reservedOrder = await orderRepo.GetByIdAsync(orderId);
            Assert.NotNull(reservedOrder);
            Assert.Equal(OrderStatus.StockReserved, reservedOrder.Status);

            // 步驟 C: 模擬收到支付成功事件 (Saga 終態推進至 Completed)
            var paymentConsumer = new PaymentProcessedConsumer(orderRepo, NullLogger<PaymentProcessedConsumer>.Instance);
            var mockPaymentCtx = new Mock<ConsumeContext<IPaymentProcessedEvent>>();
            mockPaymentCtx.Setup(c => c.Message.OrderId).Returns(orderId);
            mockPaymentCtx.Setup(c => c.Message.PaymentId).Returns("PAY-TEST-999");
            mockPaymentCtx.Setup(c => c.Message.Amount).Returns(50000);

            await paymentConsumer.Consume(mockPaymentCtx.Object);

            var completedOrder = await orderRepo.GetByIdAsync(orderId);
            Assert.NotNull(completedOrder);
            Assert.Equal(OrderStatus.Completed, completedOrder.Status);
        }

        [Fact(DisplayName = "3. 介面冪等性測試：相同 Idempotency-Key 重複發送應回傳快取結果")]
        public async Task Test_3_IdempotencyKey_ShouldReturnCachedResult_WithoutDuplicateExecution()
        {
            var redisConn = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
            var idempotencyStore = new RedisIdempotencyStore(redisConn, NullLogger<RedisIdempotencyStore>.Instance);

            const string idempotencyKey = "TEST-IDEMPOTENT-KEY-001";

            // 第一次請求：取得鎖定成功
            var lock1 = await idempotencyStore.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromMinutes(2));
            Assert.True(lock1);

            // 儲存執行結果快取
            var responseCache = new IdempotencyResponse
            {
                StatusCode = 200,
                Body = "{\"orderId\":888,\"status\":\"Success\"}",
                CreatedAt = DateTime.UtcNow
            };
            await idempotencyStore.SaveResponseAsync(idempotencyKey, responseCache, TimeSpan.FromHours(24));

            // 第二次請求（相同 Key）：取得鎖定失敗，但快取命中
            var lock2 = await idempotencyStore.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromMinutes(2));
            Assert.False(lock2);

            var cached = await idempotencyStore.GetResponseAsync(idempotencyKey);
            Assert.NotNull(cached);
            Assert.Equal(200, cached.StatusCode);
            Assert.Contains("888", cached.Body);
        }

        [Fact(DisplayName = "4. 購物車連動測試：下單事件 Consumer 應自動清除 Redis 購物車")]
        public async Task Test_4_OrderCreated_ShouldAutomaticallyClearRedisBasket()
        {
            var redisConn = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
            var basketRepo = new RedisBasketRepository(redisConn, NullLogger<RedisBasketRepository>.Instance);

            const string customerId = "USR-BASKET-TEST-001";

            // 寫入購物車項目
            var basket = new CustomerBasket(customerId)
            {
                Items = new List<BasketItem>
                {
                    new BasketItem { ProductId = "PROD-TEST-A", ProductName = "MacBook", UnitPrice = 50000, Quantity = 1 }
                }
            };
            await basketRepo.UpdateBasketAsync(basket);

            var savedBasket = await basketRepo.GetBasketAsync(customerId);
            Assert.NotNull(savedBasket);
            Assert.Single(savedBasket.Items);

            // 觸發 BasketOrderCreatedConsumer
            var orderCreatedConsumer = new BasketOrderCreatedConsumer(basketRepo, NullLogger<BasketOrderCreatedConsumer>.Instance);
            var mockOrderCtx = new Mock<ConsumeContext<IOrderCreatedEvent>>();
            mockOrderCtx.Setup(c => c.Message.OrderId).Returns(12345);
            mockOrderCtx.Setup(c => c.Message.CustomerId).Returns(customerId);

            await orderCreatedConsumer.Consume(mockOrderCtx.Object);

            // 驗證購物車已自動清除
            var clearedBasket = await basketRepo.GetBasketAsync(customerId);
            Assert.Null(clearedBasket);
        }
    }
}
