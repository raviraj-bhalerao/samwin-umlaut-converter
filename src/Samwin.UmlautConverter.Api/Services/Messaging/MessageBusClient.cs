using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Samwin.UmlautConverterLib.Step3;
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using System.Diagnostics.CodeAnalysis;

namespace Samwin.UmlautConverter.Api.Services.Messaging
{
    /// <summary>
    /// A RabbitMQ client implementation using the modern asynchronous API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MessageBusClient : IMessageBusClient, IAsyncDisposable
    {
        private readonly string _queueName = "my_demo_queue";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MetricsService _metricsService;
        private readonly ILogger<MessageBusClient> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private readonly ConnectionFactory _factory;
        private readonly IActivityService _activityService;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly Dictionary<Guid, EventHandler<(string Input, string Query, ActivityContext Context)>> _consumerMessageReceivedHandlers = new();

        private record MessageEnvelope<T>(Guid ConsumerId, T Inputs);

        public void AddConsumerMessageReceivedHandler(Guid consumerId, EventHandler<(string Input, string Query, ActivityContext Context)> handler)
        {
            if (!_consumerMessageReceivedHandlers.TryGetValue(consumerId, out var existingHandler))
            {
                _consumerMessageReceivedHandlers.Add(consumerId, handler);
            }
            else
            {
                existingHandler += handler;
                _consumerMessageReceivedHandlers[consumerId] = existingHandler;
            }
        }

        public void RemoveConsumerMessageReceivedHandler(Guid consumerId, EventHandler<(string Input, string Query, ActivityContext Context)> handler)
        {
            if (_consumerMessageReceivedHandlers.TryGetValue(consumerId, out var existingHandler))
            {
                existingHandler -= handler;
                if (existingHandler is null)
                {
                    _consumerMessageReceivedHandlers.Remove(consumerId);
                }
                else
                {
                    _consumerMessageReceivedHandlers[consumerId] = existingHandler;
                }
            }
        }

        private void RaiseMessageReceivedEvent(Guid consumerId, string input, string query, ActivityContext context)
        {
            if (_consumerMessageReceivedHandlers.TryGetValue(consumerId, out var handler))
            {
                handler?.Invoke(this, (input, query, context));
            }
        }

        public MessageBusClient(IServiceScopeFactory scopeFactory, MetricsService metricsService, ILogger<MessageBusClient> logger, IActivityService activityService)
        {
            _scopeFactory = scopeFactory;
            _metricsService = metricsService;
            _logger = logger;
            _factory = new ConnectionFactory()
            {
                Uri = new Uri(Environment.GetEnvironmentVariable("RABBIT_MQ_URI") ?? "amqp://guest:guest@localhost:5672")
            };
            _activityService = activityService;
        }

        private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
        {
            if (_channel is { IsOpen: true }) return _channel;

            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (_channel is { IsOpen: true }) return _channel;

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                _logger.LogInformation("RabbitMQ Connection and Channel established.");
                return _channel;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task PublishMessageAsync<T>(Guid consumerId, T message)
        {
            using (var publishMessageActivity = _activityService.StartActivity("PublishMessage", ActivityKind.Producer))
            {
                using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "Publish Message To Queue" } }))
                {
                    var channel = await GetChannelAsync();

                    // Declare the Queue (it's idempotent, so safe to call multiple times)
                    await channel.QueueDeclareAsync(
                        queue: _queueName, // Using routingKey as queue name for direct publishing
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var properties = new BasicProperties
                    {
                        Persistent = true,
                        Headers = new Dictionary<string, object?>()
                    };

                    // Inject the current Activity context into the message headers for distributed tracing
                    Propagator.Inject(new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
                    properties.Headers, (headers, key, value) =>
                    {
                        headers[key] = value;
                    });

                    // 2. Prepare the message
                    var messageBody = JsonSerializer.Serialize(new MessageEnvelope<T>(ConsumerId: consumerId, Inputs: message));
                    var body = Encoding.UTF8.GetBytes(messageBody);

                    // 3. Publish with basicProperties containing our trace context
                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: body);
                    _logger.LogInformation("Message published: {Message}", messageBody);
                }
            }
        }

        public async Task ConsumeMessagesAsync(CancellationToken cancellationToken)
        {

            var channel = await GetChannelAsync(cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                try
                {
                    // Extract the Activity context from the message headers
                    var parentContext = Propagator.Extract(default, ea.BasicProperties.Headers, (headers, key) =>
                    {
                        if (headers != null && headers.TryGetValue(key, out var value))
                        {
                            // Headers are often byte arrays in RabbitMQ
                            var stringValue = value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : value?.ToString();
                            return stringValue != null ? new[] { stringValue } : Enumerable.Empty<string>();
                        }
                        return Enumerable.Empty<string>();
                    });

                    var body = ea.Body.ToArray();
                    var messageBody = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<MessageEnvelope<string[]>>(messageBody);

                    if (message != null && message.ConsumerId != default(Guid) && message.Inputs != null)
                    {
                        using (var receiveMessageActivity = _activityService.StartActivity("ReceiveMessage", ActivityKind.Consumer, parentContext.ActivityContext))
                        {
                            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "ReceivedMessage" } }))
                            {
                                _logger.LogInformation("Inputs received: {Count}", message.Inputs.Length);
                                if (_consumerMessageReceivedHandlers.ContainsKey(message.ConsumerId))
                                {
                                    using (IServiceScope scope = _scopeFactory.CreateScope())
                                    {
                                        _metricsService.TokensConverted.Add(message.Inputs.Length);
                                        var sqlQueryGenerator = scope.ServiceProvider.GetRequiredService<ISqlQueryGenerator<SqlQuery>>();

                                        foreach (var input in message.Inputs)
                                        {
                                            var sqlQueries = sqlQueryGenerator.Generate([input]);
                                            _metricsService.QueriesGenerated.Add(sqlQueries.Count());
                                            foreach (var sqlQuery in sqlQueries)
                                            {
                                                using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "PublishQuery" } }))
                                                {
                                                    _metricsService.VariationsCreated.Add(sqlQuery.Parameters.Count());
                                                    await Task.Delay(TimeSpan.FromSeconds(10));
                                                    this.RaiseMessageReceivedEvent(message.ConsumerId, input, sqlQuery.ToQueryString(), receiveMessageActivity!.Context);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Message Processing skipped as no consumer registered for {id}", message.ConsumerId.ToString("D"));
                                }
                            }
                        }
                    }
                    await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.LogCritical(ex, "DI container disposed while processing message");
                    throw; // this is NOT recoverable
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RabbitMQ message - {errorMessage}", ex.Message);
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                }
            };

            await channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
            _connectionLock.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}