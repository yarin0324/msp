using Common.Contracts;

namespace OrderService.Application.Events
{
    public class OrderCreatedEvent : IOrderCreatedEvent
    {
        public long Id
        {
            get => OrderId;
            set => OrderId = value;
        }

        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public List<IOrderItemContract> Items { get; set; } = new List<IOrderItemContract>();
    }

    public class OrderItemEvent : IOrderItemContract
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
