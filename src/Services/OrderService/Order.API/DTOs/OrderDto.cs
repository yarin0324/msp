namespace OrderService.WebApi.DTOs
{
    public class OrderDto
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
