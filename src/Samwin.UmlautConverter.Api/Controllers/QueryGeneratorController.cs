using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Services.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using Samwin.UmlautConverter.Api.Services.UmlautConversion;

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
        private readonly MetricsService _metricsService;
        public QueryGeneratorController(IMessageBusClient messageBusClient, IActivityService activityService,
                             IConvertUmlaut umlautConversionService, MetricsService metricsService, ILogger<QueryGeneratorController> logger)
        {
            _messageBusClient = messageBusClient;
            _activityService = activityService;
            _umlautConversionService = umlautConversionService;
            _metricsService = metricsService;
            _logger = logger;
        }


        [HttpGet(nameof(GetQuery))]
        public async Task<ServerSentEventsResult<string>> GetQuery(
            [FromQuery(Name = "input")] string[] inputs,
            [FromQuery] bool? useCache, // Defaults to false if missing
            CancellationToken clientDisconnectedToken) // Automatically bound to the request lifetime
        {
            bool shouldCache = useCache ?? HttpContext.Request.Query.ContainsKey("useCache");
            Response.OnStarting(() =>
            {
                Response.Headers.ContentType = "text/event-stream; charset=utf-8";
                Response.Headers.CacheControl = "no-cache";
                return Task.CompletedTask;
            });

            return TypedResults.ServerSentEvents(getQueries(inputs, shouldCache, clientDisconnectedToken));


        }
        private async IAsyncEnumerable<SseItem<string>> getQueries(string[] inputs, bool useCache, [EnumeratorCancellation] CancellationToken clientDisconnectedToken)
        {
            await foreach (var query in await _umlautConversionService.Convert(inputs, useCache, clientDisconnectedToken))
            {
                yield return new SseItem<string>($"{query}", eventType: "query");
            }
        }
    }
}