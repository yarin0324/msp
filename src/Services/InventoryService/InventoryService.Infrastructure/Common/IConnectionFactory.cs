using System.Data;

namespace InventoryService.Infrastructure.Common
{
    public interface IConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync(string connectId);
    }
}