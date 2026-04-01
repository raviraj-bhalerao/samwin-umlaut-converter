using System;
using System.Threading;
using System.Threading.Tasks;
using Samwin.UmlautConverter.Api.Models;

namespace Samwin.UmlautConverter.Api.Services
{
    /// <summary>
    /// Interface for publishing messages to an external message bus.
    /// </summary>
    public interface IMessageBusClient
    {
        Task PublishMessageAsync<T>(T message);
        Task ConsumeMessagesAsync(CancellationToken cancellationToken);
        event EventHandler<string>? MessageReceived;
    }
}