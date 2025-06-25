namespace Order.Core.Events
{
    public class OrderCreatedEvent
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
