namespace OrderService.WebApi.DTOs
{
    public class GetOrderResponseDto
    {
        public long OrderId { get; set; }
        public decimal? Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
        public string Status { get; set; }
        public List<GetOrderItemResponseDto> Items { get; set; } = new List<GetOrderItemResponseDto>();
    }
}
