namespace OrderService.Domain.Entities
{
    public class OrderItem
    {
        public long OrderId { get; private set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }

        public OrderItem()
        {
        }

        public void SetOrderId(long orderId)
        {
            OrderId = orderId;
        }

        public OrderItem(string productId, int quantity)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("ProductId cannot be empty.");
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.");

            ProductId = productId;
            Quantity = quantity;
        }
    }
}
