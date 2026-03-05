using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Common.Serialization;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Messaging
{
    public sealed class ServiceBusTicketQueuePublisher : ITicketQueuePublisher, IAsyncDisposable
    {
        // Adapter infrastrutturale: implementa ITicketQueuePublisher usando Azure Service Bus.
        // Incapsula serializzazione e mapping verso ServiceBusMessage (MessageId/CorrelationId).

        private readonly ServiceBusSender _sender;

        public ServiceBusTicketQueuePublisher([FromKeyedServices("ingest")] ServiceBusSender sender)
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
                CorrelationId = ticket.CorrelationId,
                Subject = "ticket_ingested"
            };

            message.ApplicationProperties["correlationId"] = ticket.CorrelationId;
            message.ApplicationProperties["messageId"] = ticket.MessageId;
            message.ApplicationProperties["source"] = ticket.Source ?? "";

            return _sender.SendMessageAsync(message, ct);
        }
        public ValueTask DisposeAsync() => _sender.DisposeAsync();

    }

}
