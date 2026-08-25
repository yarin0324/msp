namespace Common.Contracts
{
    public interface IOrderCreatedEvent
    {
        long OrderId { get; }
        string CustomerId { get; }
        decimal Amount { get; }
        string Currency { get; }
        DateTime CreateTime { get; }
        List<IOrderItemContract> Items { get; }
    }

    public interface IOrderItemContract
    {
        string ProductId { get; }
        int Quantity { get; }
    }
}
