using Common.Contracts;

namespace InventoryService.Application.Events
{
    /// <summary>
    /// 庫存扣減成功事件
    /// </summary>
    public class InventoryDeductedEvent : IInventoryDeductedEvent
    {
        //public long OrderId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
