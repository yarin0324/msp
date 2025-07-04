namespace OrderService.Domain.Events
{
    public class OrderCreatedEvent
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
