# Microservices E-Commerce Blueprint (.NET 8 & Clean Architecture)

基於 .NET 8 與整潔架構 (Clean Architecture) 開發之企業級微服務電商系統範本。本專案採 Monorepo 架構管理，以 Ocelot 作為 API 網關進行統一路由，並導入 CQRS 模式與 RabbitMQ，透過 Choreography-based Saga Pattern 處理分散式交易與跨服務的資料最終一致性。

---

## 系統架構圖 (System Architecture)

~~~text
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
~~~

---

## 專案目錄結構 (Project Directory)

微服務、API 網關與共享事件契約 (Event Contracts) 統一收錄於 `src/` 目錄：

~~~text
msp/
├── src/
│   ├── ApiGateways/
│   │   └── WebApiGateway/          # Ocelot API 網關入口與路由配置 (ocelot.json)
│   │
│   └── Services/
│       ├── Common/
│       │   └── Contracts/          # 跨服務事件契約介面 (如：IInventoryDeductedEvent)
│       ├── Identity.API/           # 身份驗證與授權服務
│       ├── Product.API/            # 商品資訊管理服務
│       ├── BasketService/          # 購物車服務 (Clean Architecture)
│       ├── InventoryService/       # 庫存管理服務 (Clean Architecture + MQ Consumer)
│       ├── OrderService/           # 訂單處理服務 (Clean Architecture + CQRS + Saga)
│       └── Payment.API/            # 支付處理服務
│
├── docker-compose.yml              # 系統容器化編排配置
└── MSP.sln                         # 解決方案檔
~~~

---

## 核心架構與技術亮點 (Key Features)

### 1. 領域驅動與整潔架構 (Clean Architecture)
- 核心服務嚴格遵循依賴倒置原則 (Dependency Inversion Principle)，劃分為四層：
  - **Domain:** 封裝業務實體與倉儲介面，維持零外部相依。
  - **Application:** 處理業務邏輯協調與 CQRS 模式實作。
  - **Infrastructure:** 封裝資料庫存取 (EF Core) 與訊息佇列 (RabbitMQ) 等技術細節。
  - **WebApi:** 輕量化 RESTful 控制器與依賴注入 (DI) 配置。

### 2. 共享事件契約 (Event Contracts)
- 將跨服務通訊所需之事件介面抽離至 `Services/Common/Contracts`，確保發布者 (Producer) 與消費者 (Consumer) 具備強型別約束，降低微服務間的直接耦合。

### 3. CQRS 讀寫分離與 Mediator 模式
- 在 Application 層徹底分離 Commands (異動) 與 Queries (查詢) 的職責，並結合 MediatR 與 FluentValidation，實現高內聚、低耦合的請求處理管線。

### 4. Saga 補償式分散式事務 (Choreography Saga)
- 運用 RabbitMQ 實作非同步的跨服務交易流程：
  1. `OrderService` 建立訂單並發布 `OrderPendingEvent`。
  2. `InventoryService` 消費該事件並進行庫存扣減。
  3. 扣減成功則發布後續事件推進流程；若庫存不足，則發布失敗事件，觸發 `OrderService` 的退單與資料補償機制。

### 5. 高併發與系統容錯治理
- **防超賣設計：** 庫存扣減採用 SQL/Redis 進行原子操作，避免高併發環境下的 Race Condition。
- **冪等性消費：** 建立 Event Log 與 Message ID 校驗機制，防範訊息佇列的重複消費。
- **生命週期管理：** 消費端採用 BackgroundWorker 解析 Scoped 服務，避免 DI 容器產生 Captive Dependency 問題。

---

## 技術堆疊 (Tech Stack)

- **框架:** .NET 8 (C#)
- **API 網關:** Ocelot
- **架構設計:** Clean Architecture, CQRS, Event-Driven Architecture
- **訊息佇列:** RabbitMQ
- **資料儲存:** Redis, Entity Framework Core / SQL Server
- **核心套件:** FluentValidation, Scrutor, MediatR
- **部署架構:** Docker, Docker Compose

---

## 快速啟動 (Quick Start)

請確認本機已安裝 Docker Desktop 與 .NET 8 SDK：

~~~bash
# 1. 取得專案代碼
git clone https://github.com/yarin0324/msp.git
cd msp

# 2. 啟動微服務與基礎設施 (RabbitMQ / Redis / Database)
docker-compose up -d --build
~~~

服務預設連接埠：
- API 網關入口: `http://localhost:5000`
- RabbitMQ 管理介面: `http://localhost:15672` (預設憑證: guest / guest)

---

## 測試執行 (Testing)

各微服務具備相應之單元與整合測試：

~~~bash
# 執行所有測試案例
dotnet test MSP.sln
~~~
