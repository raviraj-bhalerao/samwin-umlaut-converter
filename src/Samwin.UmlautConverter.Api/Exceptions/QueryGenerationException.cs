using System;
using System.Diagnostics.CodeAnalysis;

namespace Samwin.UmlautConverter.Api.Services.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class QueryGenerationException : Exception
    {
        public QueryGenerationException(string message) : base(message) { }
    }
}