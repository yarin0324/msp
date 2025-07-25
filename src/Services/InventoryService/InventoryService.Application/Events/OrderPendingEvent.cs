namespace InventoryService.Application.Events
{
    /// <summary>
    /// 庫存扣減成功事件
    /// </summary>
    public class OrderPendingEvent
    {
        public long OrderId { get; set; }
    }
}
