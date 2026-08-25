-- ===================================================
-- MSP Microservices Database Initialization Script
-- ===================================================

-- 1. OrderDb Initialization
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'OrderDb')
BEGIN
    CREATE DATABASE OrderDb;
END
GO

USE OrderDb;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        OrderId BIGINT IDENTITY(1,1) PRIMARY KEY,
        CustomerId NVARCHAR(100) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL,
        Status INT NOT NULL DEFAULT 0, -- 0: Pending, 1: StockReserved, 2: Cancelled, 3: Completed
        CreateTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdateTime DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
BEGIN
    CREATE TABLE OrderItems (
        OrderItemId BIGINT IDENTITY(1,1) PRIMARY KEY,
        OrderId BIGINT NOT NULL,
        ProductId NVARCHAR(100) NOT NULL,
        Quantity INT NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
BEGIN
    CREATE TABLE OutboxMessages (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        OccurredOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Type NVARCHAR(255) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        ProcessedOn DATETIME2 NULL,
        Error NVARCHAR(MAX) NULL
    );
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_ProcessedOn ON OutboxMessages(ProcessedOn, OccurredOn);
END
GO

-- 2. InventoryDb Initialization
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'InventoryDb')
BEGIN
    CREATE DATABASE InventoryDb;
END
GO

USE InventoryDb;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Inventory')
BEGIN
    CREATE TABLE Inventory (
        ProductId NVARCHAR(100) PRIMARY KEY,
        Quantity INT NOT NULL DEFAULT 0,
        CreateTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdateTime DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    -- 初始種子庫存資料
    INSERT INTO Inventory (ProductId, Quantity, CreateTime, UpdateTime) VALUES 
    ('PROD-001', 100, GETUTCDATE(), GETUTCDATE()),
    ('PROD-002', 50, GETUTCDATE(), GETUTCDATE()),
    ('PROD-003', 10, GETUTCDATE(), GETUTCDATE()),
    ('PROD-LIMITED', 1, GETUTCDATE(), GETUTCDATE());
END
GO
