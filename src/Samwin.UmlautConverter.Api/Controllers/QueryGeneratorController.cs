using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;

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

        public QueryGeneratorController(ILogger<QueryGeneratorController> logger, IMessageBusClient messageBusClient, IActivityService activityService)
        {
            _logger = logger;
            _messageBusClient = messageBusClient;
            _activityService = activityService;
        }


        [HttpGet(nameof(GetQuery))]
        public async Task<ServerSentEventsResult<string>> GetQuery(
            [FromQuery(Name = "input")] string[] inputs,
            CancellationToken clientDisconnectedToken) // Automatically bound to the request lifetime
        {
            Response.OnStarting(() =>
            {
                Response.Headers.ContentType = "text/event-stream; charset=utf-8";
                Response.Headers.CacheControl = "no-cache";
                return Task.CompletedTask;
            });
            try
            {
                using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "GetQuery" }, { "NumberOfInpouts", inputs.Length }, { "OperationId", Guid.NewGuid() } }))
                {
                    await _messageBusClient.PublishMessageAsync(inputs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to message bus.");
            }

            return TypedResults.ServerSentEvents(getQueries(clientDisconnectedToken));


        }
        private async IAsyncEnumerable<SseItem<string>> getQueries([EnumeratorCancellation] CancellationToken clientDisconnectedToken)
        {
            var channel = Channel.CreateUnbounded<(string msg, ActivityContext context)>();
            void Handler(object? sender, (string Message, ActivityContext Context) data)
            {
                channel.Writer.TryWrite(data);
            }
            _messageBusClient.MessageReceived += Handler;
            try
            {
                while (true)
                {
                    // 1. Create a timeout source for 30 seconds
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    // 2. Link it with the client's disconnect token
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(clientDisconnectedToken, timeoutCts.Token);
                    using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "ReceivedMessage" } })) ;

                    (string Message, ActivityContext Context)? item;
                    try
                    {
                        // 3. Wait for the next item from the channel using the linked token
                        item = await channel.Reader.ReadAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (clientDisconnectedToken.IsCancellationRequested)
                        {
                            _logger.LogWarning("[Log] Client disconnected.");
                        }
                        else
                        {
                            _logger.LogWarning("[Log] 30s Timeout reached with no new data. Closing stream.");
                        }
                        yield break; // Exit the stream
                    }
                    using (_activityService.StartActivity("ReceivedQuery", ActivityKind.Internal, item.Value.Context)) ;

                    _logger.LogInformation($"Query received and acknowledged: {item}");
                    // 4. If we reach here, we received a message. 
                    // The 'using' block ends, the old timeout is disposed, 
                    // and the loop restarts, creating a fresh 30s timeout.
                    _logger.LogDebug($"[Log] Sending input to client: {item}");
                    yield return new SseItem<string>($"{item.Value.Message}", eventType: "query");
                }
            }
            finally
            {
                _messageBusClient.MessageReceived -= Handler;
                channel.Writer.TryComplete();
            }
        }
    }
}