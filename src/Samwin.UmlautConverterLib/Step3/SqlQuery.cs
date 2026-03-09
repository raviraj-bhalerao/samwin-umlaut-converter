using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Step3
{
    public class SqlQuery
    {
        public string Sql { get; init; } = string.Empty;
        public Dictionary<string, object> Parameters { get; init; } = new();
    }
}