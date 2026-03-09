namespace Samwin.UmlautConverterLib.Step3
{
    /// <summary>
    /// Formats SQL values safely for embedding into queries.
    /// </summary>
    /// <remarks>
    /// The `StandardSqlValueFormatter` escapes single quotes and wraps values in quotes for SQL compliance.
    ///
    /// Complexity:
    /// O(n)
    /// - n = length of the input string
    ///
    /// Advantages:
    /// - Prevents syntax errors in SQL
    /// - Simple, reusable
    ///
    /// Disadvantages:
    /// - Does not replace parameterized queries in terms of security
    ///
    /// Recommended usage:
    /// When generating plain SQL statements that embed values directly.
    /// </remarks>
    public class StandardSqlValueFormatter : ISqlValueFormatter
    {
        public string Format(string value)
        {
            if (value == null)
                return "NULL";

            var escaped = value.Replace("'", "''");
            return $"'{escaped}'";
        }
    }
}