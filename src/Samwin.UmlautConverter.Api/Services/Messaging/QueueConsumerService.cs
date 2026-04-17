using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Samwin.UmlautConverter.Api.Services.Messaging
{
    [ExcludeFromCodeCoverage]
    public class QueueConsumerService : BackgroundService
    {
        private readonly IMessageBusClient _messageBusClient;
        private readonly ILogger<QueueConsumerService> _logger;

        public QueueConsumerService(IMessageBusClient messageBusClient, ILogger<QueueConsumerService> logger)
        {
            _messageBusClient = messageBusClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue Consumer Service is starting.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Initializing RabbitMQ consumer for 'my_demo_queue'...");

                        await _messageBusClient.ConsumeMessagesAsync(stoppingToken);

                        // Wait indefinitely until the service is stopped
                        await Task.Delay(Timeout.Infinite, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // This is expected when the service is stopping
                        throw; // Re-throw to exit the loop and the service gracefully
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "RabbitMQ connection or consumption failed. Retrying in 5 seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait before retrying
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Queue Consumer Service has stopped.");
            }
        }
    }
}