using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using Weikio.ApiFramework.Plugins.MySql.Configuration;

namespace Weikio.ApiFramework.Plugins.MySql.Schema
{
    public class SchemaReader : IDisposable
    {
        private readonly MySqlOptions _options;

        private MySqlConnection _connection;

        public SchemaReader(MySqlOptions mySqlOptions)
        {
            _options = mySqlOptions;
        }

        public void Connect()
        {
            _connection = new MySqlConnection(_options.ConnectionString);
            _connection.Open();
        }

        private void RequireConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("SchemaReader is not connected to a database.");
            }
        }

        public (IList<Table> QueryCommands, SqlCommands NonQueryCommands) GetSchemaFor(SqlCommands sqlCommands)
        {
            RequireConnection();

            var queryCommands = new List<Table>();
            var nonQueryCommands = new SqlCommands();

            if (sqlCommands?.Any() != true)
            {
                return (queryCommands, nonQueryCommands);
            }

            foreach (var sqlCommand in sqlCommands)
            {
                if (sqlCommand.Value.IsNonQuery())
                {
                    if (sqlCommand.Value.IsDelete())
                    {
                        throw new NotSupportedException($"DELETE commands are not supported. Command name: '{sqlCommand.Key}'.");
                    }
                    else if (sqlCommand.Value.IsUpdate() && !sqlCommand.Value.HasWhereClause())
                    {
                        throw new InvalidOperationException("");
                    }

                    nonQueryCommands.Add(sqlCommand.Key, sqlCommand.Value);

                    // don't read schema for INSERT and UPDATE commands
                    continue;
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = sqlCommand.Value.CommandText;
                    cmd.CommandTimeout = (int) TimeSpan.FromMinutes(5).TotalSeconds;

                    if (sqlCommand.Value.Parameters != null)
                    {
                        foreach (var parameter in sqlCommand.Value.Parameters)
                        {
                            var parameterType = Type.GetType(parameter.Type);

                            if (parameterType == null)
                            {
                                throw new ArgumentException(
                                    $"SQL command '{sqlCommand.Key}' has an invalid type '{parameter.Type}' defined for parameter '{parameter.Name}'.");
                            }

                            object parameterValue = null;

                            if (parameterType.IsValueType)
                            {
                                parameterValue = Activator.CreateInstance(parameterType);
                            }
                            else if (parameterType.IsArray)
                            {
                                if (parameterType.GetElementType().IsValueType)
                                {
                                    parameterValue = Activator.CreateInstance(parameterType.GetElementType());
                                }
                            }

                            cmd.Parameters.AddWithValue($"@{parameter.Name}", parameterValue);
                        }
                    }

                    var columns = GetColumns(cmd);
                    queryCommands.Add(new Table($"{sqlCommand.Key}", "", columns, sqlCommand.Value));
                }
            }

            return (queryCommands, nonQueryCommands);
        }

        public IList<Table> ReadSchemaFromDatabaseTables()
        {
            RequireConnection();

            var schema = new List<Table>();

            var schemaTables = _connection.GetSchema("Tables");

            foreach (DataRow schemaTable in schemaTables.Rows)
            {
                if (schemaTable["TABLE_TYPE"].ToString() != "TABLE" && schemaTable["TABLE_TYPE"].ToString() != "BASE TABLE")
                {
                    continue;
                }

                var tableQualifier = "";

                if (schemaTable.Table.Columns.Contains("TABLE_QUALIFIER"))
                {
                    tableQualifier = schemaTable["TABLE_QUALIFIER"].ToString();
                }
                else if (schemaTable.Table.Columns.Contains("TABLE_SCHEMA"))
                {
                    tableQualifier = schemaTable["TABLE_SCHEMA"].ToString();
                }

                var tableName = schemaTable["TABLE_NAME"].ToString();
                var includeQualifierInName = _options.IncludeSchemaInName && !string.IsNullOrWhiteSpace(tableQualifier);

                var tableNameWithQualifier = includeQualifierInName ? $"{tableQualifier}.{tableName}" : tableName;

                if (!_options.Includes(tableName))
                {
                    continue;
                }

                using (var command = _connection.CreateCommand())
                {
                    Console.WriteLine($"Handling {tableNameWithQualifier}");
                    command.CommandText = $"select * from {tableNameWithQualifier}";
                    command.CommandTimeout = (int) TimeSpan.FromMinutes(5).TotalSeconds;

                    var columns = GetColumns(command);
                    schema.Add(new Table(tableName, tableQualifier, columns));
                    Console.WriteLine($"Handled {tableNameWithQualifier}");
                }
            }

            return schema;
        }

        public IList<Column> GetColumns(MySqlCommand cmd)
        {
            var columns = new List<Column>();

            using (var reader = cmd.ExecuteReader())
            {
                using (var dtSchema = reader.GetSchemaTable())
                {
                    if (dtSchema != null)
                    {
                        foreach (DataRow schemaColumn in dtSchema.Rows)
                        {
                            var columnName = Convert.ToString(schemaColumn["ColumnName"]);
                            var dataType = (Type) schemaColumn["DataType"];
                            var isNullable = (bool) schemaColumn["AllowDBNull"];

                            columns.Add(new Column(columnName, dataType, isNullable));
                        }
                    }
                }
            }

            return columns;
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        _connection.Dispose();
                        _connection = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
