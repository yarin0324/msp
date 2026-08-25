namespace Common.Contracts
{
    public interface IInventoryDeductedFailedEvent
    {
        long OrderId { get; set; }
        string ProductId { get; set; }
        int RequestedQuantity { get; set; }
        string Reason { get; set; }
    }
}
