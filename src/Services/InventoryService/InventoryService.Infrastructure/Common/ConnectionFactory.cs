using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace InventoryService.Infrastructure.Common
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public ConnectionFactory(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task<IDbConnection> CreateConnectionAsync(string connectId)
        {
            var connectionStringKey = connectId.ToLower() switch
            {
                "inventory" => "InventoryDb",
                _ => "" // Default Connection
            };

            var connectionString = _configuration.GetConnectionString(connectionStringKey);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception($"Connection string for {connectId} not found.");
            }

            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            return connection;
        }
    }
}