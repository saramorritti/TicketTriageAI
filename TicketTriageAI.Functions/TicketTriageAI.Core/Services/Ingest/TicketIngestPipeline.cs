using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Messaging;

namespace TicketTriageAI.Core.Services.Ingest
{
    public sealed class TicketIngestPipeline : ITicketIngestPipeline
    {
        //application service / use case “Ingest Ticket”.
        //la Function non deve contenere logica; chiami un caso d’uso.
        //coordinare mapping → publish.

        private readonly ITicketQueuePublisher _publisher;
        private readonly ITicketIngestedFactory _factory;

        public TicketIngestPipeline(ITicketQueuePublisher publisher, ITicketIngestedFactory factory)
        {
            _publisher = publisher;
            _factory = factory;
        }

        public Task ExecuteAsync(TicketIngestedRequest request, string correlationId, CancellationToken ct = default)
        {
            var ticketEvent = _factory.Create(request, correlationId);
            return _publisher.PublishAsync(ticketEvent, ct);
        }
    }
}
