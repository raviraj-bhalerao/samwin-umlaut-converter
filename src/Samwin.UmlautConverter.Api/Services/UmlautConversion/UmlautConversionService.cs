using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Services.Exceptions;
using Samwin.UmlautConverter.Api.Services.Messaging;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using Samwin.UmlautConverter.Api.Utils;

namespace Samwin.UmlautConverter.Api.Services.UmlautConversion
{
    [ExcludeFromCodeCoverage]
    public class UmlautConversionService : IConvertUmlaut
    {
        private readonly ILogger<UmlautConversionService> _logger;
        private readonly IMessageBusClient _messageBusClient;
        private readonly IActivityService _activityService;
        private readonly IMemoryCache _cache;
        private readonly MetricsService _metricsService;
        public UmlautConversionService(IMessageBusClient messageBusClient, IActivityService activityService,
            IMemoryCache cache, MetricsService metrics, ILogger<UmlautConversionService> logger)
        {
            _messageBusClient = messageBusClient;
            _activityService = activityService;
            _cache = cache;
            _metricsService = metrics;
            _logger = logger;
        }

        public async Task<IAsyncEnumerable<string>> Convert(string[] inputs, bool useCache,
            CancellationToken clientDisconnectedToken)
        {
            var consumerId = Guid.NewGuid();
            var uniqueInputs = inputs.Distinct();
            var inputsToPublish = new List<string>();
            var cachedResults = new List<string>();
            foreach (var input in uniqueInputs)
            {
                string cacheKey = $"umlautToken:{input}";
                if (useCache && _cache.TryGetValue(cacheKey, out string? cachedQuery))
                {
                    cachedResults.Add(cachedQuery!);
                    _metricsService.RecordCacheHit();
                }
                else
                {
                    inputsToPublish.Add(input);
                    if (useCache)
                    {
                        _metricsService.RecordCacheMiss();
                    }
                }
            }

            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                                        {
                                            { "Scope", "GetQuery" },
                                            { "NumberOfInputs", inputs.Length },
                                            { "CachedHits", cachedResults.Count },
                                            { "ToPublish", inputsToPublish.Count },
                                            { "OperationId", Guid.NewGuid() }
                                        }))
                {
                    if (inputsToPublish.Any())
                    {
                        await _messageBusClient.PublishMessageAsync(consumerId, inputsToPublish.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to message bus. - {errorMessage}", ex.Message);
            }

            return MergeStreams(cachedResults, getQueries(consumerId, inputsToPublish, useCache, clientDisconnectedToken));
        }

        private async IAsyncEnumerable<string> getQueries(Guid consumerId,
            List<string> expectedInputs,
            bool useCache,
            [EnumeratorCancellation] CancellationToken clientDisconnectedToken)
        {
            if (!expectedInputs.Any())
            {
                _logger.LogDebug("No inputs to process. Skipping message subscription.");
                yield break;
            }

            var channel = Channel.CreateUnbounded<(string Input, string Query, ActivityContext Context)>();

            void Handler(object? sender, (string Input, string Query, ActivityContext Context) data)
            {
                channel.Writer.TryWrite(data);
            }

            _messageBusClient.AddConsumerMessageReceivedHandler(consumerId, Handler);

            var pendingInputs = new HashSet<string>(expectedInputs);

            try
            {
                while (pendingInputs.Any())
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(clientDisconnectedToken, timeoutCts.Token);

                    (string Input, string Query, ActivityContext Context) item;

                    try
                    {
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
                            _logger.LogWarning("[Log] Timeout reached. Closing stream.");
                        }
                        yield break;
                    }

                    using (var receiveQryActivity = _activityService.StartActivity("ReceivedQuery", ActivityKind.Internal, item.Context))
                    {
                        _logger.LogInformation($"Query received: {item.Query}");

                        // ✅ Cache it
                        if (useCache)
                        {
                            string cacheKey = $"umlautToken:{item.Input}";

                            var cacheOptions = new MemoryCacheEntryOptions()
                                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1))
                                .RegisterPostEvictionCallback((key, value, reason, state) =>
                                {
                                    // This code runs when the item is removed
                                    _logger.LogWarning($"Cache entry {key} was evicted. Reason: {reason}");
                                });

                            _cache.Set(cacheKey, item.Query, cacheOptions);

                            _logger.LogInformation($"Query cached: {cacheKey} at {DateTime.Now}, will be evicted at {DateTime.Now.AddMinutes(2)}");
                        }

                        pendingInputs.Remove(item.Input);
                        if (item.Input.Equals("error", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new QueryGenerationException($"Failed to process input: {item.Input}");
                        }

                        yield return item.Query;
                    }
                }
            }
            finally
            {
                _messageBusClient.RemoveConsumerMessageReceivedHandler(consumerId, Handler);
                channel.Writer.TryComplete();
            }
        }
        private async IAsyncEnumerable<string> MergeStreams(IEnumerable<string> cached, IAsyncEnumerable<string> liveStream)
        {
            foreach (var item in cached)
            {
                yield return item;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            await foreach (var item in liveStream)
            {
                yield return item;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}