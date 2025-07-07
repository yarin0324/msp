namespace InventoryService.Domain.Entities
{
    public class Stock
    {
        public string ProductId { get; private set; }
        public int Quantity { get; private set; }

        public Stock(string productId, int quantity)
        {
            if (string.IsNullOrEmpty(productId)) throw new ArgumentException("ProductId cannot be empty.");
            if (quantity < 0) throw new ArgumentException("Quantity cannot be negative.");

            ProductId = productId;
            Quantity = quantity;
        }

        public void Reduce(int amount)
        {
            if (amount > Quantity)
                throw new Exception($"Insufficient stock for ProductId: {ProductId}. Available: {Quantity}, Requested: {amount}");
            Quantity -= amount;
        }
    }
}
