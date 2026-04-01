using System;
using System.Collections.Generic;
using System.Text;

namespace Samwin.UmlautConverterLib.Step3
{
    public class SqlQuery
    {
        public string Sql { get; init; } = string.Empty;
        public Dictionary<string, object> Parameters { get; init; } = new();
        public string ToQueryString()
        {
            if (string.IsNullOrWhiteSpace(Sql)) return string.Empty;

            var result = new StringBuilder(Sql);

            foreach (var param in Parameters)
            {
                // Ensure we handle the @ prefix consistently
                string placeholder = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";

                // Format the value based on its type
                string formattedValue = FormatValue(param.Value);

                // Replace the placeholder with the actual value
                result.Replace(placeholder, formattedValue);
            }

            return result.ToString();
        }

        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'", // Simple SQL escaping
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "1" : "0",
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}