namespace OrderService.Application.Dtos
{
    /// <summary>
    /// 作為用例輸出，返回訂單完整資料
    /// </summary>
    public class OrderResponseDto
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
        public DateTime CreateTime { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
