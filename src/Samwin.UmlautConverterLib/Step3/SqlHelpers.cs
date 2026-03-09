using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Step3
{
    public static class SqlHelpers
    {
        public static string PlainSelectStatemement(string inClause)
        {
            return $"SELECT * FROM tbl_phonebook WHERE last_name IN ({inClause});";
        }
    }
}