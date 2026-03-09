using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Step3
{
    public interface ISqlQueryGenerator<T>
    {
        IEnumerable<T> Generate(IEnumerable<string> names, bool combineAll = false);
    }
}