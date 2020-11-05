using System.IO;
using System.Text.RegularExpressions;

namespace Weikio.ApiFramework.Plugins.MySql.Configuration
{
    public class SqlCommand
    {
        private string _commandTextFile;
        public string CommandText { get; set; }

        public string CommandTextFile
        {
            get { return _commandTextFile; }
            set
            {
                _commandTextFile = value;

                if (!string.IsNullOrEmpty(_commandTextFile))
                {
                    CommandText = File.ReadAllText(_commandTextFile);
                }
            }
        }

        public string DataTypeName { get; set; }

        public SqlCommandParameter[] Parameters { get; set; }

        public string GetEscapedCommandText()
        {
            return CommandText.Replace("\"", "\"\"");
        }

        public bool IsQuery()
        {
            return Is("SELECT");
        }

        public bool IsNonQuery()
        {
            return !IsQuery();
        }

        public bool IsInsert()
        {
            return Is("INSERT");
        }

        public bool IsUpdate()
        {
            return Is("UPDATE");
        }

        public bool IsDelete()
        {
            return Is("DELETE");
        }

        private bool Is(string operation)
        {
            return Regex.IsMatch(CommandText, $@"^\s*{operation}\s", RegexOptions.IgnoreCase);
        }

        public bool HasWhereClause()
        {
            return Regex.IsMatch(CommandText, @"\sWHERE\s", RegexOptions.IgnoreCase);
        }
    }
}
