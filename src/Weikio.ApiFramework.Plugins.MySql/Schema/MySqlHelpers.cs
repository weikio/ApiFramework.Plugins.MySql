using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace Weikio.ApiFramework.Plugins.MySql.Schema
{
    public static class MySqlHelpers
    {
        // Regex for finding an IN operator having just one parameter placeholder (?).
        // Ignores whitespace and is case insensitive.
        private static readonly Regex _inOperatorRegex = new Regex(@"\s+IN\s*\(\s*\?\s*\)", RegexOptions.IgnoreCase);

        public static (string, MySqlParameter[]) CreateQuery(string tableName, int? top, List<string> fields)
        {
            var sqlQuery =
                $"SELECT {(fields?.Any() == true ? string.Join(",", fields.Select(f => f.ToUpper())) : " * ")} FROM {tableName} {(top.GetValueOrDefault() > 0 ? " Limit " + top.ToString() : "")} ";

            return (sqlQuery, new MySqlParameter[] { });
        }

        public static void AddParameter(MySqlCommand mysqlCommand, string name, object value)
        {
            if (value?.GetType().IsArray == true && _inOperatorRegex.IsMatch(mysqlCommand.CommandText ?? ""))
            {
                var arrayValues = (object[])value;
                var arrayParameterNumber = 1;

                foreach (var parameterValue in arrayValues)
                {
                    var parameterName = $"@{name}_{arrayParameterNumber}";
                    mysqlCommand.Parameters.AddWithValue(parameterName, parameterValue);

                    arrayParameterNumber += 1;
                }

                // Replace the single placeholder in IN function with the correct amount of placeholders.
                // For example: "STATUS IN (?)"  -->  "STATUS IN(?, ?, ?)"
                var parameterPlaceholders = string.Join(", ", Enumerable.Repeat("?", arrayValues.Length));
                mysqlCommand.CommandText = _inOperatorRegex.Replace(mysqlCommand.CommandText, $" IN({parameterPlaceholders})", count: 1);
            }
            else
            { 
                mysqlCommand.Parameters.AddWithValue($"@{name}", value);
            }
        }
    }
}
