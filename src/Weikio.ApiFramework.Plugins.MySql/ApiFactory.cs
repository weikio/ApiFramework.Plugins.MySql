using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SqlKata.Compilers;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.MySql
{
    public class ApiFactory : DatabaseApiFactoryBase
    {
        public ApiFactory(ILogger<ApiFactory> logger, ILoggerFactory loggerFactory) : base(logger, loggerFactory)
        {
        }

        public List<Type> Create(MySqlOptions configuration)
        {
            var pluginSettings = new DatabasePluginSettings(config => new MySqlConnectionCreator(config), 
                tableName => $"select * from {tableName} limit 0",
                new MySqlCompiler());

            var result = Generate(configuration, pluginSettings);

            return result;
        }
    }
}
