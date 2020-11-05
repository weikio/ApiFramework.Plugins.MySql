using System;
using System.Collections.Generic;
using System.Linq;

namespace Weikio.ApiFramework.Plugins.MySql.Configuration
{
    public class MySqlOptions
    {
        public string ConnectionString { get; set; }

        public string[] Tables { get; set; }

        public bool IncludeSchemaInName { get; set; } = true;
        
        public bool Includes(string tableName)
        {
            if (Tables?.Any() != true)
            {
                return true;
            }

            return Tables.Contains(tableName, StringComparer.OrdinalIgnoreCase);
        }

        public bool ShouldGenerateApisForTables()
        {
            if (Tables == null)
            {
                return true;
            }

            if (Tables.Length == 1 && Tables.First() == "")
            {
                return false;
            }

            return true;
        }

        public SqlCommands SqlCommands { get; set; }
    }
}
