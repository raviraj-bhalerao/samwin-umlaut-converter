using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Samwin.UmlautConverter.Api.ResponseManagement
{
    [ExcludeFromCodeCoverage]
    public static class ApiResponseFactory
    {
        public static ApiResponse<T> Success<T>(T data, HttpContext context) =>
            new()
            {
                Success = true,
                Data = data,
                TraceId = Activity.Current?.TraceId != null ? Activity.Current.TraceId.ToString() : context.TraceIdentifier
            };

        public static ApiResponse<T> Failure<T>(string message, HttpContext context, List<string>? errors = null) =>
            new()
            {
                Success = false,
                Message = message,
                TraceId = Activity.Current?.TraceId != null ? Activity.Current.TraceId.ToString() : context.TraceIdentifier,
                Errors  = errors
            };
    }
}