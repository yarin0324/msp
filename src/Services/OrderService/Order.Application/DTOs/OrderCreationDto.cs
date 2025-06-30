namespace Order.Adapters.DTOs
{
    public class OrderCreationDto
    {
        public decimal? Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
    }
}
