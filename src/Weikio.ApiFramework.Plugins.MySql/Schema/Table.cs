using System.Collections.Generic;
using Weikio.ApiFramework.Plugins.MySql.Configuration;

namespace Weikio.ApiFramework.Plugins.MySql.Schema
{
    public class Table
    {
        public string Name { get; }

        public string Qualifier { get; }

        public string NameWithQualifier { get; }

        public IList<Column> Columns { get; }

        public SqlCommand SqlCommand { get; set; }

        public Table(string name, string qualifier, IList<Column> columns, SqlCommand sqlCommand = null)
        {
            Name = name;
            Qualifier = qualifier;
            NameWithQualifier = $"{qualifier}.{name}";
            Columns = columns ?? new List<Column>();
            SqlCommand = sqlCommand;
        }
    }
}
