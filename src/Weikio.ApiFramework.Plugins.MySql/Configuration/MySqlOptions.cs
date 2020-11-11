using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Weikio.ApiFramework.Plugins.MySql.Configuration
{
    public class MySqlOptions
    {
        public string ConnectionString { get; set; }

        public string[] Tables { get; set; }
        public string[] ExcludedTables { get; set; }

        public bool IncludeSchemaInName { get; set; } = true;

        public bool Includes(string tableName)
        {
            if (ExcludedTables?.Any() != true && Tables?.Any() != true)
            {
                return true;
            }

            if (ExcludedTables?.Any() == true)
            {
                foreach (var excludedTable in ExcludedTables)
                {
                    var regEx = NameToRegex(excludedTable);

                    if (regEx.IsMatch(tableName))
                    {
                        return false;
                    }
                }
            }

            if (Tables?.Any() != true)
            {
                return true;
            }

            foreach (var table in Tables)
            {
                var regEx = NameToRegex(table);

                if (regEx.IsMatch(tableName))
                {
                    return true;
                }
            }

            return false;
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

        private static Regex NameToRegex(string nameFilter)
        {
            // https://stackoverflow.com/a/30300521/66988
            var regex = "^" + Regex.Escape(nameFilter).Replace("\\?", ".").Replace("\\*", ".*") + "$";

            return new Regex(regex, RegexOptions.Compiled);
        }
    }
}
