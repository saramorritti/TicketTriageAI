using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Messaging
{
    public sealed class ServiceBusTicketQueuePublisher : ITicketQueuePublisher
    {
        // Adapter infrastrutturale: implementa ITicketQueuePublisher usando Azure Service Bus.
        // Incapsula serializzazione e mapping verso ServiceBusMessage (MessageId/CorrelationId).

        private readonly ServiceBusSender _sender;

        public ServiceBusTicketQueuePublisher(ServiceBusClient client)
        {
            _sender = client.CreateSender("tickets-ingest");
        }

        public Task PublishAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(ticket);

            var message = new ServiceBusMessage(payload)
            {
                ContentType = "application/json",
                MessageId = ticket.MessageId,
                CorrelationId = ticket.CorrelationId
            };

            return _sender.SendMessageAsync(message, ct);
        }
    }

}
