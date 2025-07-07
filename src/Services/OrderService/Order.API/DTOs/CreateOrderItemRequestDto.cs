namespace OrderService.WebApi.DTOs
{
    public class CreateOrderItemRequestDto
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
