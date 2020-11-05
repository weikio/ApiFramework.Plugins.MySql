using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.Plugins.MySql.Schema;

namespace Weikio.ApiFramework.Plugins.MySql.CodeGeneration
{
    public class CodeGenerator
    {
        public Assembly GenerateAssembly(IList<Table> querySchema, SqlCommands nonQueryCommands, MySqlOptions mySqlOptions)
        {
            var gen = new Weikio.TypeGenerator.CodeToAssemblyGenerator();
            gen.ReferenceAssembly(typeof(System.Console).Assembly);
            gen.ReferenceAssembly(typeof(System.Data.DataRow).Assembly);
            gen.ReferenceAssembly(typeof(MySqlDateTime).Assembly);
            gen.ReferenceAssembly(typeof(MySqlCommand).Assembly);
            gen.ReferenceAssembly(typeof(MySqlHelpers).Assembly);

            var assemblyCode = GenerateCode(querySchema, nonQueryCommands, mySqlOptions);

            try
            {
                var result = gen.GenerateAssembly(assemblyCode);

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }
        }

        public string GenerateCode(IList<Table> querySchema, SqlCommands nonQueryCommands, MySqlOptions mySqlOptions)
        {
            var sb = new StringBuilder();
            sb.UsingNamespace("System");
            sb.UsingNamespace("System.Collections.Generic");
            sb.UsingNamespace("System.Data");
            sb.UsingNamespace("MySql.Data");
            sb.UsingNamespace("MySql.Data.MySqlClient");
            sb.UsingNamespace("MySql.Data.Types");
            sb.UsingNamespace("System.Reflection");
            sb.UsingNamespace("System.Linq");
            sb.UsingNamespace("System.Diagnostics");
            sb.UsingNamespace("Weikio.ApiFramework.Plugins.MySql.Configuration");
            sb.UsingNamespace("Weikio.ApiFramework.Plugins.MySql.Schema");
            sb.WriteLine("");

            foreach (var table in querySchema)
            {
                sb.WriteNamespaceBlock(table, namespaceBlock =>
                {
                    namespaceBlock.WriteDataTypeClass(table);

                    namespaceBlock.WriteQueryApiClass(table, mySqlOptions);
                });
            }

            foreach (var command in nonQueryCommands)
            {
                sb.WriteNamespaceBlock(command, namespaceBlock =>
                {
                    namespaceBlock.WriteNonQueryCommandApiClass(command, mySqlOptions);
                });
            }

            return sb.ToString();
        }
    }

    public static class StringBuilderExtensions
    {
        public static void UsingNamespace(this StringBuilder sb, string ns)
        {
            sb.AppendLine($"using {ns};");
        }

        public static void WriteLine(this StringBuilder sb, string text)
        {
            sb.AppendLine(text);
        }

        public static void Write(this StringBuilder sb, string text)
        {
            sb.Append(text);
        }



        public static void Namespace(this StringBuilder sb, string ns)
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        public static void FinishBlock(this StringBuilder sb)
        {
            sb.AppendLine("}");
        }

        public static void StartClass(this StringBuilder sb, string className)
        {
            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");
        }

        public static void UsingBlock(this StringBuilder writer, string declaration, Action<StringBuilder> inner)
        {
            writer.Write("using (" + declaration + ")");
            writer.Write("{");
            inner(writer);
            writer.FinishBlock();
        }


    }
}
