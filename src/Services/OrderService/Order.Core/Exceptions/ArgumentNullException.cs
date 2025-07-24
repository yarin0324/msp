namespace OrderService.Domain.Exceptions
{
    public class ArgumentNullException : Exception
    {
        public string ErrorCode { get; set; }

        public ArgumentNullException(string message, string errorCode = "ArgumentNull") : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public ArgumentNullException(string message, Exception innerException, string errorCode = "ArgumentNull") : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }
    }
}
