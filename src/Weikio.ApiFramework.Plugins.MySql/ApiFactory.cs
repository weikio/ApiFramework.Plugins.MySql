using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weikio.ApiFramework.Plugins.MySql.CodeGeneration;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.Plugins.MySql.Schema;

namespace Weikio.ApiFramework.Plugins.MySql
{
    public static class ApiFactory
    {
        public static Task<IEnumerable<Type>> Create(MySqlOptions configuration)
        {
            var querySchema = new List<Table>();
            var nonQueryCommands = new SqlCommands();

            using (var schemaReader = new SchemaReader(configuration))
            {
                schemaReader.Connect();

                if (configuration.SqlCommands != null)
                {
                    var commandsSchema = schemaReader.GetSchemaFor(configuration.SqlCommands);

                    querySchema.AddRange(commandsSchema.QueryCommands);
                    nonQueryCommands = commandsSchema.NonQueryCommands;
                }

                if (configuration.ShouldGenerateApisForTables())
                {
                    var dbTables = schemaReader.ReadSchemaFromDatabaseTables();
                    querySchema.AddRange(dbTables);
                }
            }

            var generator = new CodeGenerator();
            var assembly = generator.GenerateAssembly(querySchema, nonQueryCommands, configuration);

            var result = assembly.GetExportedTypes()
                .Where(x => x.Name.EndsWith("Api"))
                .ToList();

            return Task.FromResult<IEnumerable<Type>>(result);
        }
    }
}
