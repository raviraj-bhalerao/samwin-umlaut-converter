using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Samwin.UmlautConverter.Api.Models;

namespace Samwin.UmlautConverter.Api.Services.Messaging
{
    /// <summary>
    /// Interface for publishing messages to an external message bus.
    /// </summary>
    public interface IMessageBusClient
    {
        Task PublishMessageAsync<T>(Guid consumerId, T message);
        Task ConsumeMessagesAsync(CancellationToken cancellationToken);
        void AddConsumerMessageReceivedHandler(Guid consumerId, EventHandler<(string Input, string Query, ActivityContext Context)> handler);
        void RemoveConsumerMessageReceivedHandler(Guid consumerId, EventHandler<(string Input, string Query, ActivityContext Context)> handler);
    }
}