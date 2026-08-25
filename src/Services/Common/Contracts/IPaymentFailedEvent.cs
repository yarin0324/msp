namespace Common.Contracts
{
    public interface IPaymentFailedEvent
    {
        long OrderId { get; }
        decimal Amount { get; }
        string Reason { get; }
    }
}
