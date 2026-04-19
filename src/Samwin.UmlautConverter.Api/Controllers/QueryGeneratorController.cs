using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Services.Messaging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using Samwin.UmlautConverter.Api.Services.UmlautConversion;
using Samwin.UmlautConverter.Api.ResponseManagement;
using System;

namespace Samwin.UmlautConverter.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ExcludeFromCodeCoverage]
    public class QueryGeneratorController : ControllerBase
    {
        private readonly ILogger<QueryGeneratorController> _logger;
        private readonly IMessageBusClient _messageBusClient;
        private readonly IActivityService _activityService;
        private readonly IConvertUmlaut _umlautConversionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MetricsService _metricsService;
        public QueryGeneratorController(IMessageBusClient messageBusClient, IActivityService activityService
                             , IConvertUmlaut umlautConversionService, MetricsService metricsService
                             , IHttpContextAccessor httpContextAccessor, ILogger<QueryGeneratorController> logger)
        {
            _messageBusClient = messageBusClient;
            _activityService = activityService;
            _umlautConversionService = umlautConversionService;
            _metricsService = metricsService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        [HttpGet(nameof(GetQuery))]
        [SseEndpoint]
        public async Task<ServerSentEventsResult<ApiResponse<string>>> GetQuery(
            [FromQuery(Name = "input")] string[] inputs,
            [FromQuery] bool? useCache, // Defaults to false if missing
            CancellationToken clientDisconnectedToken) // Automatically bound to the request lifetime
        {

            bool shouldCache = useCache ?? HttpContext.Request.Query.ContainsKey(nameof(useCache));

            return TypedResults.ServerSentEvents(getQueries(inputs, shouldCache, clientDisconnectedToken));

        }
        private async IAsyncEnumerable<SseItem<ApiResponse<string>>> getQueries(string[] inputs, bool useCache, [EnumeratorCancellation] CancellationToken clientDisconnectedToken)
        {
            IAsyncEnumerable<string>? stream = null;
            Exception? exception = null;

            try
            {
                // 1. Protect the service call
                stream = await _umlautConversionService.Convert(inputs, useCache, clientDisconnectedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stream failed to start");
                exception = ex;
            }
            if (exception != null)
            {
                yield return new SseItem<ApiResponse<string>>(
                        ApiResponseFactory.Failure<string>("Stream failed to start", _httpContextAccessor.HttpContext!, new List<string> { exception.Message }));
                yield break;
            }

            // 3. If the stream was successfully created, iterate outside the try/catch
            if (stream != null)
            {
                IAsyncEnumerator<string> enumerator = stream.GetAsyncEnumerator(clientDisconnectedToken);

                // We use a manual while loop to wrap the MoveNext in a try/catch 
                // while keeping the yield in a safe zone.
                bool hasNext = true;
                while (hasNext)
                {
                    string? current = null;
                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                        if (hasNext) current = enumerator.Current;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Stream error");
                        exception = ex;
                    }
                    if (exception != null)
                    {
                        yield return new SseItem<ApiResponse<string>>(
                                ApiResponseFactory.Failure<string>("Stream error", _httpContextAccessor.HttpContext!, new List<string> { exception.Message }));
                        yield break;
                    }

                    if (hasNext && current != null)
                    {
                        yield return new SseItem<ApiResponse<string>>(
                                ApiResponseFactory.Success(current, _httpContextAccessor.HttpContext!));
                    }
                }

                await enumerator.DisposeAsync();
            }
        }
    }
}