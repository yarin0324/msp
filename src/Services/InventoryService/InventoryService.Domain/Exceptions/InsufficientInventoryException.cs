namespace InventoryService.Domain.Exceptions
{
    /// <summary>
    /// 庫存不足異常
    /// </summary>
    public class InsufficientInventoryException : Exception
    {
        public string ErrorCode { get; set; }

        public InsufficientInventoryException(string message, string errorCode = "InsufficientInventory") : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public InsufficientInventoryException(string message, Exception innerException, string errorCode = "InsufficientInventory") : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }
    }
}
