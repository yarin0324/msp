namespace OrderService.Application.Dtos
{
    /// <summary>
    /// CreateOrderCommand 子物件，用於傳遞訂單項目資料
    /// </summary>
    public class OrderItemDto
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
