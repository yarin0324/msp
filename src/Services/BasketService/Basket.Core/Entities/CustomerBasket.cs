namespace Basket.Core.Entities
{
    public class CustomerBasket
    {
        public string CustomerId { get; set; } = string.Empty;
        public List<BasketItem> Items { get; set; } = new();

        public decimal TotalPrice => Items.Sum(item => item.UnitPrice * item.Quantity);

        public CustomerBasket() { }

        public CustomerBasket(string customerId)
        {
            CustomerId = customerId;
        }
    }
}
