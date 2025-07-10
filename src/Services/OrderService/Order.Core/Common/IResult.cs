namespace OrderService.Domain.Common
{
    public interface IResult<TResponse>
    {
        bool IsSuccess { get; }
        TResponse Value { get; }
        string Message { get; }
    }
}
