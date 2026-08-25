namespace Common.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> TryAcquireLockAsync(string key, TimeSpan lockExpiry);
        Task<IdempotencyResponse?> GetResponseAsync(string key);
        Task SaveResponseAsync(string key, IdempotencyResponse response, TimeSpan dataExpiry);
        Task ReleaseLockAsync(string key);
    }
}
