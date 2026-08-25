namespace Common.Contracts
{
    public interface IPaymentProcessedEvent
    {
        long OrderId { get; }
        string PaymentId { get; }
        decimal Amount { get; }
        DateTime ProcessedTime { get; }
    }
}
