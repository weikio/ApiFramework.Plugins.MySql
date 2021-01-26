using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.Plugins.MySql.Schema;
using Weikio.TypeGenerator.Types;

namespace Weikio.ApiFramework.Plugins.MySql.CodeGeneration
{
    public static class SourceWriterExtensions
    {
        public static void WriteNamespaceBlock(this StringBuilder sb, Table table,
            Action<StringBuilder> contentProvider)
        {
            sb.Namespace(typeof(ApiFactory).Namespace + ".Generated" + table.Name);

            contentProvider.Invoke(sb);

            sb.FinishBlock(); // Finish the namespace
        }
        
        public static void WriteNamespaceBlock(this StringBuilder writer, KeyValuePair<string, SqlCommand> command,
            Action<StringBuilder> contentProvider)
        {
            writer.Namespace(typeof(ApiFactory).Namespace + ".Generated" + command.Key);

            contentProvider.Invoke(writer);

            writer.FinishBlock(); // Finish the namespace
        }

        public static void WriteDataTypeClass(this StringBuilder writer, Table table)
        {
            writer.StartClass($"{GetDataTypeName(table)}");

            foreach (var column in table.Columns)
            {
                var typeName = TypeToTypeWrapper.GetFriendlyName(column.Type, column.Type.Name);
                writer.WriteLine($"public {typeName} {GetPropertyName(column.Name)} {{ get;set; }}");
            }

            writer.WriteLine("");

            writer.WriteLine("public object this[string propertyName]");
            writer.WriteLine("{");
            writer.WriteLine("get{return this.GetType().GetProperty(propertyName).GetValue(this, null);}");
            writer.WriteLine("set{this.GetType().GetProperty(propertyName).SetValue(this, value, null);}");
            writer.FinishBlock(); // Finish the this-block

            writer.FinishBlock(); // Finish the class
        }

        public static void WriteQueryApiClass(this StringBuilder writer, Table table, MySqlOptions mySqlOptions)
        {
            writer.StartClass(GetApiClassName(table));

            writer.WriteLine($"private readonly ILogger<{GetApiClassName(table)}> _logger;");
            writer.WriteLine($"public {GetApiClassName(table)} (ILogger<{GetApiClassName(table)}> logger)");
            writer.WriteLine("{");
            writer.WriteLine("_logger = logger;");
            writer.WriteLine("}");
            
            var columnMap = new Dictionary<string, string>();

            foreach (var column in table.Columns)
            {
                columnMap.Add(column.Name, GetPropertyName(column.Name));
            }

            writer.Write("public static Dictionary<string, string> ColumnMap = new Dictionary<string, string>()");
            writer.Write("{");

            foreach (var columnPair in columnMap)
            {
                writer.Write($"    {{\"{columnPair.Key}\", \"{columnPair.Value}\"}},");
            }

            writer.WriteLine("};");
            writer.WriteLine("");

            writer.WriteLine("public MySqlOptions Configuration { get; set; }");

            if (table.SqlCommand != null)
            {
                writer.WriteCommandMethod(table.Name, table.SqlCommand, mySqlOptions);
            }
            else
            {
                writer.WriteDefaultTableQueryMethod(table, mySqlOptions);
            }

            writer.FinishBlock(); // Finish the class
        }

        public static void WriteNonQueryCommandApiClass(this StringBuilder writer, KeyValuePair<string, SqlCommand> command, MySqlOptions mySqlOptions)
        {
            writer.StartClass(GetApiClassName(command));

            writer.WriteLine("public MySqlOptions Configuration { get; set; }");

            writer.WriteCommandMethod(command.Key, command.Value, mySqlOptions);

            writer.FinishBlock(); // Finish the class
        }

        private static void WriteCommandMethod(this StringBuilder writer, string commandName, SqlCommand sqlCommand, MySqlOptions mySqlOptions)
        {
            var sqlMethod = sqlCommand.CommandText.Trim()
                .Split(new[] { ' ' }, 2)
                .First().ToLower();
            sqlMethod = sqlMethod.Substring(0, 1).ToUpper() + sqlMethod.Substring(1);

            var methodParameters = new List<string>();

            if (sqlCommand.Parameters != null)
            {
                foreach (var sqlCommandParameter in sqlCommand.Parameters)
                {
                    var methodParam = "";

                    if (sqlCommandParameter.Optional)
                    {
                        var paramType = Type.GetType(sqlCommandParameter.Type);

                        if (paramType.IsValueType)
                        {
                            methodParam += $"{sqlCommandParameter.Type}? {sqlCommandParameter.Name} = null";
                        }
                        else
                        {
                            methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name} = null";
                        }
                    }
                    else
                    {
                        methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name}";
                    }

                    methodParameters.Add(methodParam);
                }
            }

            var dataTypeName = sqlCommand.IsNonQuery() ? "int" : GetDataTypeName(commandName, sqlCommand);
            var returnType = sqlCommand.IsNonQuery() ? "int" : $"List<{dataTypeName}>";

            writer.WriteLine($"public {returnType} {sqlMethod}({string.Join(", ", methodParameters)})");
            writer.WriteLine("{");

            if (sqlCommand.IsQuery())
            {
                writer.WriteLine($"var result = new List<{dataTypeName}>();");
            }
            else
            { 
                writer.WriteLine($"{returnType} result;");
            }
                        
            writer.WriteLine("");

            writer.UsingBlock($"var conn = new MySqlConnection(\"{mySqlOptions.ConnectionString}\")", w =>
            {
                w.WriteLine("conn.Open();");

                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine($"cmd.CommandText = @\"{sqlCommand.GetEscapedCommandText()}\";");

                    if (sqlCommand.Parameters != null)
                    {
                        foreach (var sqlCommandParameter in sqlCommand.Parameters)
                        {
                            cmdBlock.WriteLine(@$"MySqlHelpers.AddParameter(cmd, ""{sqlCommandParameter.Name}"", {sqlCommandParameter.Name});");
                        }
                    }

                    if (sqlCommand.IsQuery())
                    {
                        cmdBlock.UsingBlock("var reader = cmd.ExecuteReader()", readerBlock =>
                        {
                            readerBlock.WriteLine("while (reader.Read())");
                            readerBlock.WriteLine("{");
                            readerBlock.WriteLine($"var item = new {dataTypeName}();");
                            readerBlock.WriteLine("foreach (var column in ColumnMap)");
                            readerBlock.WriteLine("{");

                            readerBlock.Write(
                                "item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];");
                            readerBlock.FinishBlock(); // Finish the column setting foreach loop

                            readerBlock.Write("result.Add(item);");
                            readerBlock.FinishBlock(); // Finish the while loop
                        });
                    }
                    else
                    {
                        cmdBlock.WriteLine("result = cmd.ExecuteNonQuery();");
                    }
                });
            });

            writer.Write("return result;");
            writer.FinishBlock(); // Finish the method
        }

        private static void WriteDefaultTableQueryMethod(this StringBuilder writer, Table table,
            MySqlOptions mySqlOptions)
        {
            var dataTypeName = GetDataTypeName(table);

            writer.WriteLine($"public async IAsyncEnumerable<{dataTypeName}> Select(int? top)");
            writer.WriteLine("{");
            writer.WriteLine("try{");
            writer.WriteLine("var fields = new List<string>();");
            writer.WriteLine("");

            writer.UsingBlock($"var conn = new MySqlConnection(Configuration.ConnectionString)", w =>
            {
                w.WriteLine("await conn.OpenAsync();");

                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine(
                        $"var queryAndParameters = MySqlHelpers.CreateQuery(\"{table.NameWithQualifier}\", top, fields);");
                    cmdBlock.WriteLine("var query = queryAndParameters.Item1;");
                    cmdBlock.WriteLine("cmd.Parameters.AddRange(queryAndParameters.Item2);");

                    cmdBlock.WriteLine("cmd.CommandText = query;");
                    cmdBlock.WriteLine("_logger.LogDebug(\"Executing query: {Query}\", cmd.CommandText);");
                    cmdBlock.WriteLine("var sw = new System.Diagnostics.Stopwatch();");
                    cmdBlock.WriteLine("var rowcount = 0;");
                    cmdBlock.WriteLine("sw.Start();");

                    cmdBlock.UsingBlock("var reader = await cmd.ExecuteReaderAsync()", readerBlock =>
                    {
                        readerBlock.WriteLine("while (await reader.ReadAsync())");
                        readerBlock.WriteLine("{");
                        readerBlock.WriteLine($"var item = new {dataTypeName}();");
                        readerBlock.WriteLine("var selectedColumns = ColumnMap;");
                        readerBlock.WriteLine("if (fields?.Any() == true)");
                        readerBlock.WriteLine("{");

                        readerBlock.Write(
                            "selectedColumns = ColumnMap.Where(x => fields.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToDictionary(p => p.Key, p => p.Value);");
                        readerBlock.FinishBlock(); // Finish the if block
                        readerBlock.WriteLine("foreach (var column in selectedColumns)");
                        readerBlock.WriteLine("{");

                        readerBlock.Write(
                            "item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];");
                        readerBlock.FinishBlock(); // Finish the column setting foreach loop

                        readerBlock.WriteLine("_logger.LogTrace(\"Yielding item\");");
                        readerBlock.Write("yield return item;");
                        readerBlock.Write("rowcount += 1;");
                        readerBlock.FinishBlock(); // Finish the while loop
                    });
                    
                    cmdBlock.WriteLine("sw.Stop();");
                    cmdBlock.WriteLine("_logger.LogTrace(\"Query took {ElapsedTime} and {RowCount} rows were found.\", sw.Elapsed, rowcount);");
                });
            });
            writer.WriteLine("}");
            writer.WriteLine("catch (Exception e)");
            writer.WriteLine("{");
            writer.WriteLine("_logger.LogError(e, \"Failed to read data.\");");
            writer.WriteLine("}");
            

            writer.FinishBlock(); // Finish the method
        }

        private static string GetApiClassName(Table table)
        {
            return $"{table.Name}Api";
        }

        private static string GetApiClassName(KeyValuePair<string, SqlCommand> command)
        {
            return $"{command.Key}Api";
        }

        private static string GetDataTypeName(Table table)
        {
            if (!string.IsNullOrEmpty(table.SqlCommand?.DataTypeName))
            {
                return table.SqlCommand.DataTypeName;
            }

            return table.Name + "Item";
        }

        private static string GetDataTypeName(string commandName, SqlCommand sqlCommand = null)
        {
            if (!string.IsNullOrEmpty(sqlCommand?.DataTypeName))
            {
                return sqlCommand.DataTypeName;
            }

            return commandName + "Item";
        }

        private static string GetPropertyName(string originalName)
        {
            var isValid = IsValid(originalName);

            if (isValid)
            {
                return originalName;
            }

            var result = originalName;

            if (result.Contains(" "))
            {
                result = result.Replace(" ", "").Trim();
            }

            if (IsValid(originalName))
            {
                return result;
            }

            return $"@{result}";
        }

        private static bool IsValid(string originalName)
        {
            var keywordKind = SyntaxFacts.GetKeywordKind(originalName);
            var isValid = SyntaxFacts.IsValidIdentifier(originalName) && SyntaxFacts.IsReservedKeyword(keywordKind) == false;

            return isValid;
        }
    }
}
