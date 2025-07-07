namespace OrderService.WebApi.DTOs
{
    public class GetOrderItemResponseDto
    {
        public GetOrderItemResponseDto()
        {

        }

        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
