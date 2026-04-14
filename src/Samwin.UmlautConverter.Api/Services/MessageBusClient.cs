using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Samwin.UmlautConverterLib.Step3;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Samwin.UmlautConverter.Api.Services
{
    /// <summary>
    /// A RabbitMQ client implementation using the modern asynchronous API.
    /// </summary>
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

        public event EventHandler<(string Message, ActivityContext Context)>? MessageReceived;

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

        public async Task PublishMessageAsync<T>(T message)
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
                    Propagator.Inject(new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current), properties.Headers, (headers, key, value) =>
                    {
                        headers[key] = value;
                    });

                    // 2. Prepare the message
                    var messageBody = JsonSerializer.Serialize(message);
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
                    var message = Encoding.UTF8.GetString(body);
                    var inputs = JsonSerializer.Deserialize<string[]>(message);

                    if (inputs != null)
                    {
                        using (var receiveMessageActivity = _activityService.StartActivity("ReceiveMessage", ActivityKind.Consumer, parentContext.ActivityContext))
                        {
                            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "ReceivedMessage" } }))
                            {
                                _logger.LogInformation("Messages received and acknowledged: {Message}", inputs.Length);

                                using (IServiceScope scope = _scopeFactory.CreateScope())
                                {
                                    _metricsService.TokensConverted.Add(inputs.Length);
                                    var sqlQueryGenerator = scope.ServiceProvider.GetRequiredService<ISqlQueryGenerator<SqlQuery>>();
                                    
                                    foreach (var input in inputs)
                                    {
                                        var sqlQueries = sqlQueryGenerator.Generate([input]);
                                        _metricsService.QueriesGenerated.Add(sqlQueries.Count());
                                        foreach (var sqlQuery in sqlQueries)
                                        {
                                            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "PublishQuery" } }))
                                            {
                                                _metricsService.VariationsCreated.Add(sqlQuery.Parameters.Count());
                                            await Task.Delay(TimeSpan.FromSeconds(10));
                                            MessageReceived?.Invoke(this, (sqlQuery.ToQueryString(), receiveMessageActivity!.Context));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RabbitMQ message.");
                    // Nack and requeue the message
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