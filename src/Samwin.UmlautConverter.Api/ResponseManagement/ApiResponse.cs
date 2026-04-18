using System.Diagnostics.CodeAnalysis;

namespace Samwin.UmlautConverter.Api.ResponseManagement
{
    [ExcludeFromCodeCoverage]
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public object? Errors { get; set; } // optional (validation, etc.)
    }
}