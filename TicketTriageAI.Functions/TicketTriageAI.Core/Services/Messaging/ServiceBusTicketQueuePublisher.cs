using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Common.Serialization;

namespace TicketTriageAI.Core.Services.Messaging
{
    public sealed class ServiceBusTicketQueuePublisher : ITicketQueuePublisher, IAsyncDisposable
    {
        // Adapter infrastrutturale: implementa ITicketQueuePublisher usando Azure Service Bus.
        // Incapsula serializzazione e mapping verso ServiceBusMessage (MessageId/CorrelationId).

        private readonly ServiceBusSender _sender;

        public ServiceBusTicketQueuePublisher(ServiceBusSender sender)
        {
            _sender = sender;
        }
        public Task PublishAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(ticket, JsonDefaults.Options);

            var message = new ServiceBusMessage(payload)
            {
                ContentType = "application/json",
                MessageId = ticket.MessageId,
                CorrelationId = ticket.CorrelationId
            };

            return _sender.SendMessageAsync(message, ct);
        }
        public ValueTask DisposeAsync() => _sender.DisposeAsync();

    }

}
