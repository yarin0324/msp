namespace Order.Core.Entities
{
    /// <summary>
    /// 訂單
    /// 實體層，僅包含業務邏輯與資料結構，保持純粹性
    /// </summary>
    public class ProductOrder
    {
        public long Id { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime CreateTime { get; private set; }

        public ProductOrder(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("輸入金額須大於0");
            }

            this.Amount = amount;
            this.CreateTime = DateTime.Now;
        }
    }
}
