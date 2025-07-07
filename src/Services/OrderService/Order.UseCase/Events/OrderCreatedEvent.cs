namespace OrderService.Application.Events
{
    public class OrderCreatedEvent
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
        public DateTime CreateTime { get; set; }
        public List<OrderItemEvent> Items { get; set; } = new List<OrderItemEvent>();
    }

    public class OrderItemEvent
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
