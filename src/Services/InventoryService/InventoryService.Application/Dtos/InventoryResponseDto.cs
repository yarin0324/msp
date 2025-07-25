namespace InventoryService.Application.Dtos
{
    /// <summary>
    /// 作為用例輸出，返回庫存完整資料
    /// </summary>
    public class InventoryResponseDto
    {
        public string ProductId { get; set; }
        //public decimal Amount { get; set; }
        //public string Currency { get; set; }
        //public string CustomerId { get; set; }
        //public DateTime CreateTime { get; set; }
        //public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
