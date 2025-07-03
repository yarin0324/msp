namespace OrderService.Domain.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Message { get; }

        private Result(T value, string message)
        {
            IsSuccess = true;
            Value = value;
            Message = message;
        }

        private Result(string message)
        {
            IsSuccess = false;
            Value = default;
            Message = message;
        }

        public static Result<T> Success(T value, string message = null) => new Result<T>(value, message);
        public static Result<T> Failure(string message) => new Result<T>(message);
    }
}
