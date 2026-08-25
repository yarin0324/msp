namespace InventoryService.Domain.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public string ErrorCode { get; set; }
        public T Value { get; }
        public T Data => Value;
        public string Message { get; }

        private Result(T value, string message)
        {
            IsSuccess = true;
            Value = value;
            Message = message;
        }

        private Result(string errorCode, string message)
        {
            IsSuccess = false;
            ErrorCode = errorCode;
            Value = default;
            Message = message;
        }

        public static Result<T> Success(T value, string message = null) => new Result<T>(value, message);
        public static Result<T> Failure(string errorCode, string message = null) => new Result<T>(errorCode, message);
    }
}
