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
- 將跨服務通訊所需之事件介面抽離至 `Services/Common/Contracts`，確保發布者 (Producer) 與消費者 (Consumer) 具備強型別約束
