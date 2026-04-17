using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Services.Messaging;
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
using Samwin.UmlautConverter.Api.Services.Telemetry;

namespace Samwin.UmlautConverter.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ExcludeFromCodeCoverage]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMessageBusClient _messageBusClient;
        private readonly MetricsService _metricsService;

        public WeatherForecastController(IMessageBusClient messageBusClient, MetricsService metricsService
                                        , ILogger<WeatherForecastController> logger)
        {
            _messageBusClient = messageBusClient;
            _metricsService = metricsService;
            _logger = logger;
        }

        [HttpGet]
        public WeatherForecast[] Get()
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "WeatherForecast" }, { "OperationId", Guid.NewGuid() } }))
            {
                _logger.LogInformation("Weather report composing 🚀");
                WeatherForecast[] toRet = getWeatherForecast();
                _logger.LogInformation("Weather report composed 🚀");
                return toRet;
            }
        }
        [HttpGet("SecureGet")]
        [Authorize]
        public WeatherForecast[] SecureGet()
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "WeatherForecast.Secure" }, { "OperationId", Guid.NewGuid() } }))
            {
                _logger.LogInformation("Secure Weather report composing 🚀");
                WeatherForecast[] toRet = getWeatherForecast();
                _logger.LogInformation("Secure Weather report composed 🚀");
                return toRet;
            }
        }
        WeatherForecast[] getWeatherForecast()
        {
            return Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    Summaries[Random.Shared.Next(Summaries.Length)]
                ))
                .ToArray();
        }

        [HttpGet("GetHeartBeat")]
        public async Task<ServerSentEventsResult<string>> GetHeartBeat(CancellationToken cancellationToken)
        {
            return TypedResults.ServerSentEvents(Beats(cancellationToken));
        }
        private async IAsyncEnumerable<SseItem<string>> Beats([EnumeratorCancellation] CancellationToken token)
        {
            var rand = new Random();
            var idx = 0;
            while (!token.IsCancellationRequested)
            {
                yield return new SseItem<string>($"Heartbeat #{++idx}: {rand.Next(50, 100)} bpm", eventType: "heartbeat");
                await Task.Delay(1500, token);
            }
        }
        [HttpGet("QueueMsg")]
        public async Task<ServerSentEventsResult<string>> QueueMsg(
            [FromQuery(Name = "input")] string[] inputs,
            CancellationToken clientDisconnectedToken) // Automatically bound to the request lifetime
        {
            var consumerId = Guid.NewGuid();
            try
            {
                await _messageBusClient.PublishMessageAsync(consumerId, inputs);
                var operationId = Guid.NewGuid().ToString();
                using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "QueueMsg" }, { "OperationId", operationId } }))
                {
                    await _messageBusClient.PublishMessageAsync(consumerId, inputs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to message bus.");
            }

            return TypedResults.ServerSentEvents(getVariations(consumerId, clientDisconnectedToken));


        }
        private async IAsyncEnumerable<SseItem<string>> getVariations(Guid consumerId, [EnumeratorCancellation] CancellationToken clientDisconnectedToken)
        {
            var channel = Channel.CreateUnbounded<(string input, string qry, ActivityContext context)>();
            void Handler(object? sender, (string input, string qry, ActivityContext Context) data)
            {
                channel.Writer.TryWrite(data);
            }
            _messageBusClient.AddConsumerMessageReceivedHandler(consumerId, Handler);
            try
            {
                while (true)
                {
                    // 1. Create a timeout source for 30 seconds
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    // 2. Link it with the client's disconnect token
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(clientDisconnectedToken, timeoutCts.Token);
                    using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "ReceivedMessage" } })) ;

                    (string Input, string Query, ActivityContext Context)? item;
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

                    _logger.LogInformation($"Query received and acknowledged: {item}");
                    // 4. If we reach here, we received a message. 
                    // The 'using' block ends, the old timeout is disposed, 
                    // and the loop restarts, creating a fresh 30s timeout.
                    _logger.LogDebug($"[Log] Sending input to client: {item}");
                    yield return new SseItem<string>($"{item.Value.Query}", eventType: "query");
                }
            }
            finally
            {
                _messageBusClient.RemoveConsumerMessageReceivedHandler(consumerId, Handler);
                channel.Writer.TryComplete();
            }
        }
    }

    [ExcludeFromCodeCoverage]
    public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}