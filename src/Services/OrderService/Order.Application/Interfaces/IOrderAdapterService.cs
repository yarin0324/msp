namespace Order.UseCase.Interfaces
{
    public interface IOrderAdapterService
    {
        Task<Result<OrderDto>> CreateOrderAsync(OrderCreationDto? creation);
        Task<Result<OrderDto>> GetOrderAsync(long id);
    }
}
