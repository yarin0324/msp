# Micro Service PoC (msp)

本專案為基於 .NET 8 與 Clean Architecture 建構之分散式微服務概念驗證（Proof of Concept）架構。系統聚焦於高並行交易情境下的資料一致性、反向代理網關整合、全鏈路分散式追蹤、集中式驗證授權、介面冪等性防護以及訊息佇列容錯治理。

---

## 系統拓撲架構

```text
                                [ Client / Web / App ]
                                          │
                                          ▼ (HTTP / HTTPS)
                             ┌────────────────────────┐
                             │     WebApiGateway      │ (Microsoft YARP 2.2 + JWT 驗證)
                             └────────────┬───────────┘
                                          │ (Claims 轉發標頭: X-User-*)
       ┌──────────────┬───────────────────┼───────────────────┬──────────────┐
       ▼              ▼                   ▼                   ▼              ▼
┌──────────────┐┌──────────────┐   ┌──────────────┐    ┌──────────────┐┌──────────────┐
│ Identity.API ││ Product.API  │   │BasketService │    │ OrderService ││InventoryServ.│
│ (權杖簽發)   ││ (商品型錄)   │   │(Redis 購物車)│    │(CQRS+Outbox) ││(原子列鎖扣庫)│
└──────────────┘└──────────────┘   └──────────────┘    └──────┬───────┘└──────┬───────┘
                                                              │ (事件驅動)   │
                                                              ▼              ▼
                                                     ┌────────────────────────────────┐
                                                     │         RabbitMQ 3             │
                                                     │ (指數退避重試 + _error 死信佇列)│
                                                     └────────────────────────────────┘
```

---

## 服務與通訊埠配置

| 服務名稱 | 職責與架構層級 | 容器埠 | 對外映射埠 | 核心技術與模式 |
| :--- | :--- | :---: | :---: | :--- |
| WebApiGateway | API 反向代理網關 | 5000 | 5000 | Microsoft YARP 2.2、JWT Bearer 集中驗證、Claims 轉發、動態路由重載 |
| Identity.API | 身分識別與權杖服務 | 5184 | 5184 | HMAC-SHA256 JWT 簽發、角色授權（Admin / Customer） |
| OrderService | 訂單核心服務 | 5125 | 5125 | Clean Architecture、MediatR CQRS、Transactional Outbox、Saga 狀態機 |
| InventoryService | 庫存核心服務 | 5271 | 5271 | Clean Architecture、資料庫列鎖原子更新 SQL、MassTransit Consumer |
| BasketService | 購物車暫存服務 | 5079 | 5079 | StackExchange.Redis、TTL 過期機制、下單事件驅動自動清除 |
| Payment.API | 支付模擬服務 | 5198 | 5198 | 介面冪等性防護、支付事件發布、Saga 終態閉環 |
| Product.API | 商品查詢服務 | 5272 | 5272 | RESTful API、OpenTelemetry 鏈路追蹤 |
| SQL Server 2022 | 關聯式資料庫 | 1433 | 1433 | OrderDb、InventoryDb、db-init 自動結構遷移與種子資料匯入 |
| RabbitMQ 3 | 訊息中介佇列 | 5672 / 15672 | 5672 / 15672 | 指數退避重試（Exponential Backoff）、死信佇列（DLQ） |
| Redis 7 | 記憶體快取與鎖定 | 6379 | 6379 | 購物車狀態存取、SETNX 分散式冪等性鎖定 |
| Consul 1.16 | 服務註冊與發現 | 8500 | 8500 | 服務健康檢查探針（Health Probe）與服務註冊 |
| Jaeger Tracing | 分散式鏈路追蹤 | 16686 / 4317 | 16686 / 4317 | OpenTelemetry OTLP gRPC 收集器、全鏈路呼叫追蹤視覺化 |

---

## 核心架構設計與實作機制

### 1. 高並行庫存原子扣減（Anti-Overselling via Row-Level Locking）
* **問題成因**：傳統「先 SELECT 查詢、記憶體比對、再 UPDATE 覆寫」之寫法，在多執行緒或多節點並行競爭下必定產生 Race Condition 導致超賣。
* **技術解法**：放棄記憶體判定，改採單一原子 SQL 條件更新：
  ```sql
  UPDATE Inventory 
  SET Quantity = Quantity - @Quantity, UpdateTime = @UpdateTime 
  WHERE ProductId = @ProductId AND Quantity >= @Quantity;
  ```
  直接運用 SQL Server 資料庫底層之列層級獨占鎖（Row-level Exclusive Lock），由儲存引擎確保檢查與扣減之原子性。影響筆數（Rows Affected）為 0 即代表庫存不足，無任何交易洩漏或逾賣風險。

### 2. 雙寫一致性防護（Transactional Outbox Pattern）
* **問題成因**：在微服務中「先寫入資料庫、再發布訊息至訊息佇列」存在雙寫（Dual-Write）風險。若訊息佇列瞬斷或連線失敗，訊息將永久丟失，破壞跨服務資料一致性。
* **技術解法**：在訂單寫入時，將業務資料實體與事件酬載（OutboxMessage）包裝於同一個資料庫本機交易（Local Database Transaction）內原子寫入。後續由背景背景服務（`OutboxPublisherWorker`）定期輪詢未處理之訊息並派送至 RabbitMQ，保證至少一次傳遞（At-Least-Once Delivery）。

### 3. 分散式交易 Saga 閉環與補償機制（Choreography Saga）
* **正向流程**：
  1. `OrderService` 建立訂單（狀態：`Pending`），發布 `IOrderCreatedEvent`。
  2. `InventoryService` 消費事件並執行原子扣減，扣減成功後發布 `IInventoryDeductedEvent`。
  3. `OrderService` 接收扣減成功通知，將訂單狀態推進至 `StockReserved`。
  4. `Payment.API` 扣款成功並發布 `IPaymentProcessedEvent`，`OrderService` 將訂單標記為 `Completed`。
* **反向補償流程**：
  * 當 `InventoryService` 扣減失敗時，發布 `IInventoryDeductedFailedEvent`。
  * `OrderService` 接收失敗事件後，自動將訂單狀態轉移至 `Cancelled`，完成分散式交易補償。

### 4. API 網關集中驗證與 Claims 轉發（YARP + JWT）
* 網關採用微軟官方維護之 **YARP 2.2 (Yet Another Reverse Proxy)**。
* 於網關層集中配置 JWT Bearer 驗證中介軟體。通過驗證之請求，經由自訂之 `ClaimsTransformProvider` 提取 `sub`、`role`、`email` 等 Claims，轉換為內部專用標頭（`X-User-Id`、`X-User-Roles`、`X-User-Email`）轉發至下游。
* 下游微服務透過 `ICurrentUserContext` 直接讀取標頭，免除重複解析與驗證 JWT 之運算開銷。

### 5. 介面冪等性防護（Idempotency Key via Redis SETNX）
* 控制器動作方法標註 `[Idempotent(ExpiryHours = 24)]`。
* 接收請求時讀取 `Idempotency-Key` 標頭，利用 Redis `SETNX` 建立暫態處理鎖（In-Flight Lock，TTL 2 分鐘）。
* 執行完成後將回應之 HTTP 狀態碼與 JSON 酬載快取至 Redis（TTL 24 小時）。
* 相同 Key 於時效內重複呼叫時，直接由過濾器返回快取之結果（帶有 `X-Cache-Lookup: HIT-IDEMPOTENT` 標頭），阻斷重送請求對資料庫造成重複寫入。

### 6. 全鏈路分散式追蹤（OpenTelemetry + W3C TraceContext）
* 所有微服務與網關均注入 OpenTelemetry .NET SDK。
* 跨 HTTP 轉發與 MassTransit RabbitMQ 訊息傳遞均自動傳播 W3C `traceparent` 追蹤上下文。
* 追蹤遙測資料以 OTLP (gRPC) 協定統一匯出至 Jaeger 收集器（通訊埠 `4317`），提供跨服務呼叫鏈路之毫秒級延遲分析。

### 7. 彈性容錯與死信佇列治理（Polly & RabbitMQ DLQ）
* **HTTP 彈性容錯**：配置 Polly 斷路器（Circuit Breaker）與抖動指數退避重試（Retry with Jitter）。連續 5 次非暫態錯誤即開啟斷路器 30 秒，防止下游故障引發雪崩效應。
* **訊息重試與死信**：MassTransit 統一配置 3 次指數退避重試（1s, 4s, 10s）。重試耗盡後，訊息自動轉移至 `{queue-name}_error` 死信佇列（Dead Letter Queue），確保異常資料可被追溯與重送。

---

## 專案結構

```text
msp/
├── db-init/                        # SQL Server 初始化腳本 (init.sql / entrypoint.sh)
├── docker-compose.yml              # 系統容器化編排檔案
├── MSP.sln                         # 方案檔
│
└── src/
    ├── ApiGateways/
    │   └── WebApiGateway/          # YARP 反向代理網關、JWT 集中驗證、Claims 轉發器
    │
    └── Services/
        ├── Common/                 # 共用架構函式庫 (Contracts, Observability, Security, Idempotency, Resilience, Messaging)
        ├── Identity.API/           # 身分驗證服務 (JWT 簽發與登入)
        ├── Product.API/            # 商品型錄服務
        ├── BasketService/          # 購物車服務 (Clean Architecture + Redis)
        ├── InventoryService/       # 庫存服務 (Clean Architecture + 原子扣減)
        ├── OrderService/           # 訂單服務 (Clean Architecture + CQRS + Outbox)
        └── Payment.API/            # 支付服務 (冪等性扣款 + 事件發布)
```

---

## 快速啟動指南

### 環境需求
* Docker Desktop 4.x+
* .NET 8.0 SDK（本機開發或編譯時使用）

### 啟動容器化微服務叢集
```bash
docker-compose up -d --build
```
> `db-init` 容器將於資料庫就緒後自動建立資料表結構，並載入預設之庫存種子資料。

---

## 端到端 API 驗證程序

網關統一生效入口位址：`http://localhost:5000`

### 1. 身分驗證與取得權杖
```http
POST http://localhost:5000/identity/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password123"
}
```

### 2. 購物車新增項目（Redis 存取）
```http
POST http://localhost:5000/basket
Authorization: Bearer <Token>
Content-Type: application/json

{
  "customerId": "USR-ADMIN-001",
  "items": [
    {
      "productId": "PROD-A",
      "productName": "MacBook Pro M3",
      "unitPrice": 50000,
      "quantity": 1
    }
  ]
}
```

### 3. 建立訂單（含介面冪等性標頭）
```http
POST http://localhost:5000/order
Authorization: Bearer <Token>
Idempotency-Key: ORD-TEST-2026-001
Content-Type: application/json

{
  "customerId": "USR-ADMIN-001",
  "items": [
    {
      "productId": "PROD-A",
      "unitPrice": 50000,
      "quantity": 1
    }
  ]
}
```
* 驗證冪等性：以相同 `Idempotency-Key` 重複發送，伺服器立即回傳先前快取之回應（標頭註記 `X-Cache-Lookup: HIT-IDEMPOTENT`），資料庫與訊息佇列不重複執行。
* 驗證購物車連動：查詢 `GET /basket/USR-ADMIN-001`，購物車已由事件消費者自動清除。

### 4. 模擬支付扣款（Saga 終態推進）
```http
POST http://localhost:5000/payment/pay
Authorization: Bearer <Token>
Idempotency-Key: PAY-TEST-2026-001
Content-Type: application/json

{
  "orderId": 1,
  "amount": 50000,
  "paymentMethod": "CreditCard"
}
```
> 支付完成後發布 `IPaymentProcessedEvent`，訂單狀態自動轉變為 `Completed`。

### 5. 檢視分散式鏈路追蹤
開啟瀏覽器至 **`http://localhost:16686`**（Jaeger UI），選取服務名稱（例如 `WebApiGateway` 或 `OrderService`），即可觀察包含 HTTP 呼叫、資料庫查詢與 RabbitMQ 非同步事件之完整呼叫鏈路與耗時。

---

## 授權條款
本專案基於 MIT 授權條款開源發布。
