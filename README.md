# Microservices E-Commerce Blueprint (.NET 8 & Clean Architecture)

基於 **.NET 8** 與 **Clean Architecture** 打造的高效能分散式微服務電商系統範本。本專案採用 **Monorepo** 架構管理，透過 **Ocelot API Gateway** 進行統一路由，並整合 **CQRS** 模式與 **RabbitMQ** 實作 **Choreography-based Saga Pattern**，精確處理分散式交易與資料最終一致性。

---

## 🏛️ 系統架構圖 (System Architecture)
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
