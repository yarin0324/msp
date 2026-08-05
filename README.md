# Microservices E-Commerce Blueprint (.NET 8 & Clean Architecture)

基於 **.NET 8** 與 **Clean Architecture** 打造的高效能分散式微服務電商系統範本。本專案採用 **Monorepo** 架構管理，透過 **Ocelot API Gateway** 進行統一路由，並整合 **CQRS** 模式與 **RabbitMQ** 實作 **Choreography-based Saga Pattern**，精確處理分散式交易與資料最終一致性。

---

## System Architecture (系統架構圖)

```text
                                +-------------------+
                                |   Client / Web    |
                                +---------+---------+
                                          |
                                          v
                                +-------------------+
                                |   WebApiGateway   |  (Ocelot API Gateway)
                                +---------+---------+
                                          |
       +-----------------+----------------+-----------------+-----------------+
       |                 |                |                 |                 |
       v                 v                v                 v                 v
+--------------+  +--------------+  +--------------+  +--------------+  +--------------+
| Identity.API |  | Product.API  |  |BasketService |  | OrderService |  |InventoryServ.|
+--------------+  +--------------+  +--------------+  +-------+------+  +-------+------+
                                                                  |                 |
                                                                  +--------+--------+
                                                                           |
                                                                           v
                                                                  +-----------------+
                                                                  | RabbitMQ Broker | (Saga Pattern)
                                                                  +-----------------+
```

---

## Project Directory (專案目錄結構)

本專案將微服務、API 網關與共享事件契約收錄於 src 目錄下：

```text
msp/
├── src/
│   ├── ApiGateways/
│   │   └── WebApiGateway/          # Ocelot API Gateway 入口與路由映射 (ocelot.json)
│   │
│   └── Services/
│       ├── Common/
│       │   └── Contracts/          # 跨服務事件契約 (例如：IInventoryDeductedEvent)
│       ├── Identity.API/           # 會員驗證與身份授權服務
│       ├── Product.API/            # 商品目錄與資訊管理服務
│       ├── BasketService/          # 購物車服務 (Clean Architecture 分層)
│       ├── InventoryService/       # 庫存管理服務 (Clean Architecture + MQ Consumer)
│       ├── OrderService/           # 訂單處理服務 (Clean Architecture + CQRS + Saga)
│       └── Payment.API/            # 支付處理服務
│
├── docker-compose.yml              # 全系統容器化編排
└── MSP.sln                          # Solution 方案檔
```

---

## Key Features & Architecture (核心架構亮點)

### 1. 領域驅動與整潔架構 (Clean Architecture)
- 核心微服務嚴格遵循 **Dependency Inversion Principle (DIP)**，獨立劃分為：
  - **Domain:** 純粹的業務實體與倉儲介面，無外部套件相依。
  - **Application:** CQRS 業務邏輯協調與命令處理。
  - **Infrastructure:** 資料庫存取 (EF Core) 與 RabbitMQ 訊息發布/消費具體實作。
  - **WebApi:** RESTful Controller 端點與依賴注入組態。

### 2. 共享事件契約 (Event Contracts)
- 跨服務通訊之事件介面統一收錄於 Services/Common/Contracts，避免微服務間強耦合，同時確保 Producer 與 Consumer 之間的 Event Schema 強型別一致性。

### 3. CQRS 讀寫分離與 Mediator 模式
- 於 Application 層將 Commands 與 Queries 職責分離，搭配 **MediatR** 與 **FluentValidation** 實現請求驗證與 Pipeline 處理，遵循單一職責原則 (SRP)。

### 4. Saga 補償式分散式事務 (Choreography Saga)
- 透過 **RabbitMQ** 實作非同步事件編排：
  1. OrderService 建立訂單並發布 OrderPendingEvent。
  2. InventoryService 消費事件並執行庫存扣減。
  3. 若扣減成功，發布 IInventoryDeductedEvent 推進訂單至下個階段；若庫存不足，則發布失敗事件觸發訂單取消與復原補償機制。

### 5. 生產級併發與容錯治理
- **原子性扣減：** 庫存扣減採用 SQL/Redis 原子操作，杜絕高併發下的 Race Condition 與超賣問題。
- **消費冪等性：** 引入 Event Log 與 Message ID 機制，防範 RabbitMQ 重複消費。
- **避免 Captive Dependency：** 消費者採用 BackgroundWorker 解析 Scoped 服務，確保 DbContext 生命週期健康。

---

## Tech Stack (技術棧)

- **Framework:** .NET 8 (C#)
- **API Gateway:** Ocelot
- **Architecture:** Clean Architecture, CQRS, Event-Driven Architecture (EDA)
- **Messaging Broker:** RabbitMQ
- **Caching & DB:** Redis, Entity Framework Core / SQL Server
- **Libraries:** FluentValidation, Scrutor, MediatR
- **Containerization:** Docker, Docker Compose

---

## Quick Start (快速啟動)

確保本機已安裝 **Docker Desktop** 與 **.NET 8 SDK**：

```bash
# 1. Clone 專案
git clone [https://github.com/yarin0324/msp.git](https://github.com/yarin0324/msp.git)
cd msp

# 2. 一鍵啟動所有微服務與基礎設施 (RabbitMQ / Redis / DB)
docker-compose up -d --build
```

**預設服務埠口：**
- API Gateway 入口: http://localhost:5000
- RabbitMQ 管理介面: http://localhost:15672 (預設帳密: guest / guest)

---

## Testing (測試執行)

各微服務均包含獨立之單元測試與整合測試：

```bash
# 執行方案內所有單元與整合測試
dotnet test MSP.sln
```
