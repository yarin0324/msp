using System.Data;

namespace OrderService.Infrastructure.Common
{
    public interface IConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync(string connectId);
    }
}