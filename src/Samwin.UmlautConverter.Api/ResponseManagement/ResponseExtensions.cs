using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Samwin.UmlautConverter.Api.ResponseManagement
{
    [ExcludeFromCodeCoverage]
    public static class ResponseExtensions
    {
        public static bool IsSSEEndPoint(this HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var isSse =
                endpoint?.Metadata.GetMetadata<SseEndpointAttribute>() != null;
            return isSse;
        }
    }
}