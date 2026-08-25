using Common.Contracts;

namespace InventoryService.Application.Events
{
    /// <summary>
    /// 庫存扣減失敗事件
    /// </summary>
    public class InventoryDeductedFailedEvent : IInventoryDeductedFailedEvent
    {
        public long OrderId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
