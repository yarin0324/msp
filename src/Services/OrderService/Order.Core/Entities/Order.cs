namespace OrderService.Domain.Entities
{
    /// <summary>
    /// 訂單
    /// 實體層，僅包含業務邏輯與資料結構，保持純粹性
    /// </summary>
    public class Order
    {
        public long OrderId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }
        public string CustomerId { get; private set; }
        public DateTime CreateTime { get; private set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        public void SetOrderId(long orderId)
        {
            OrderId = orderId;
        }

        public Order() { }

        public Order(decimal amount, string currency, string customerId, List<OrderItem> items)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("輸入金額須大於0");
            }

            if (items == null || !items.Any())
            {
                throw new InvalidOperationException("訂單至少包含一筆項目");
            }

            this.Amount = amount;
            this.Currency = currency;
            this.CustomerId = customerId;
            this.CreateTime = DateTime.Now;
            this.Items = items ?? new List<OrderItem>();
        }
    }
}
