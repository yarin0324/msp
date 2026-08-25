namespace Common.Idempotency
{
    /// <summary>
    /// 冪等性快取回應結構
    /// </summary>
    public class IdempotencyResponse
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "application/json; charset=utf-8";
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
