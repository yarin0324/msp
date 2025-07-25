namespace OrderService.WebApi.DTOs
{
    public class CreateOrderRequestDto
    {
        public decimal? Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
        public List<CreateOrderItemRequestDto> Items { get; set; } = new List<CreateOrderItemRequestDto>();
    }
}