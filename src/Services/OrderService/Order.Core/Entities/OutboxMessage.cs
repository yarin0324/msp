namespace OrderService.Domain.Entities
{
    /// <summary>
    /// Transactional Outbox 訊息實體
    /// 保證資料庫交易與訊息佇列發布的雙寫一致性 (At-least-once Delivery)
    /// </summary>
    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public DateTime OccurredOn { get; private set; }
        public string Type { get; private set; } = string.Empty;
        public string Payload { get; private set; } = string.Empty;
        public DateTime? ProcessedOn { get; private set; }
        public string? Error { get; private set; }

        public OutboxMessage() { }

        public OutboxMessage(string type, string payload)
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
            Type = type;
            Payload = payload;
            ProcessedOn = null;
            Error = null;
        }

        public void MarkAsProcessed()
        {
            ProcessedOn = DateTime.UtcNow;
            Error = null;
        }

        public void MarkAsFailed(string error)
        {
            Error = error;
        }
    }
}
