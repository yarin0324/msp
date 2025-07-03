namespace OrderService.Domain.UseCase
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event) where T : class;
    }
}
