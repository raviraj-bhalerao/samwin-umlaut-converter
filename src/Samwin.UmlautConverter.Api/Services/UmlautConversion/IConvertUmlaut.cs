using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Samwin.UmlautConverter.Api.Services.UmlautConversion
{
    public interface IConvertUmlaut
    {
        Task<IAsyncEnumerable<string>> Convert(string[] inputs, bool useCache, CancellationToken clientDisconnectedToken);
    }
}