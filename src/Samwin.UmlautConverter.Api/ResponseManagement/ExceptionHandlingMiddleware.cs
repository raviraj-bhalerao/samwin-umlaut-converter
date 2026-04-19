using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Samwin.UmlautConverter.Api.ResponseManagement
{
    [ExcludeFromCodeCoverage]
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private const string SSE_CONTENT_TYPE = "text/event-stream";

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                // Skip SSE
                if (context.Response.ContentType?.Contains(SSE_CONTENT_TYPE) == true)
                {
                    // You cannot "recover" SSE here properly
                    context.Abort();
                    return;
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                
                var response = ApiResponseFactory.Failure<string>("Server Error", context, new List<string>(){ex.Message});

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}