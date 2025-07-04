namespace OrderService.Domain.Entities
{
    /// <summary>
    /// 訂單
    /// 實體層，僅包含業務邏輯與資料結構，保持純粹性
    /// </summary>
    public class ProductOrder
    {
        public long Id { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }
        public string CustomerId { get; private set; }
        public DateTime CreateTime { get; private set; }

        public ProductOrder(decimal amount, string currency, string customerId)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("輸入金額須大於0");
            }

            this.Amount = amount;
            this.Currency = currency;
            this.CustomerId = customerId;
            this.CreateTime = DateTime.Now;
        }
    }
}
