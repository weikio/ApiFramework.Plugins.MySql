using System.Data.Common;
using MySql.Data.MySqlClient;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.MySql
{
    public class MySqlConnectionCreator : IConnectionCreator
    {
        private readonly DatabaseOptionsBase _configuration;

        public MySqlConnectionCreator(DatabaseOptionsBase configuration)
        {
            _configuration = configuration;
        }

        public DbConnection CreateConnection(DatabaseOptionsBase options)
        {
            var result = new MySqlConnection(options.ConnectionString);

            return result;
        }
    }
}
