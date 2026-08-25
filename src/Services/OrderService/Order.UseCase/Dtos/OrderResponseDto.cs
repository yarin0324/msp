namespace OrderService.Application.Dtos
{
    /// <summary>
    /// 作為用例輸出，返回訂單完整資料
    /// </summary>
    public class OrderResponseDto
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
