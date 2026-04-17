using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Samwin.UmlautConverter.Api.Services.Messaging
{
    [ExcludeFromCodeCoverage]
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MetricsService _metricsService;

        private readonly ILogger<MetricsMiddleware> _logger;

        public MetricsMiddleware(RequestDelegate next, MetricsService metricsService, ILogger<MetricsMiddleware> logger)
        {
            _next = next;
            _metricsService = metricsService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                var endpoint = context.GetEndpoint();
                var statusCode = context.Response.StatusCode;
                var tags = new TagList
                            {
                                { "method", context.Request.Method },
                                { "status_code", statusCode },
                                // Grouping by category makes alerting very easy
                                { "error_type", statusCode >= 500 ? "server" : (statusCode >= 400 ? "client" : "none") }
                            };

                if (endpoint != null)
                {
                    // Logic for matched routes
                    var endpointName = endpoint.Metadata
                        .GetMetadata<EndpointNameMetadata>()?.EndpointName
                        ?? endpoint.DisplayName;

                    tags.Add("endpoint", endpointName);
                }
                else
                {
                    // Logic for unmatched routes: use the Path
                    // Example: "/api/old-service" or "/favicon.ico"
                    tags.Add("endpoint", "unmatched");

                    _logger.LogWarning(
                        $"Unmatched route accessed: {context.Request.Method} {context.Request.Path}");
                }

                _metricsService.RequestCounter.Add(1, tags);
            }
        }

    }
}