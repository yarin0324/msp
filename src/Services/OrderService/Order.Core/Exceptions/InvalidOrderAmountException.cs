namespace OrderService.Domain.Exceptions
{
    public class InvalidOrderAmountException : Exception
    {
        public string ErrorCode { get; set; }

        public InvalidOrderAmountException(string message, string errorCode = "InvalidOrderAmount") : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public InvalidOrderAmountException(string message, Exception innerException, string errorCode = "InvalidOrderAmount") : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }
    }
}
