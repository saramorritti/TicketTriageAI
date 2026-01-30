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
using TicketTriageAI.Core.Services.Processing;

namespace TicketTriageAI.Core.Services.Ingest
{
    public sealed class TicketIngestPipeline : ITicketIngestPipeline
    {
        //application service / use case “Ingest Ticket”.
        //la Function non deve contenere logica; chiami un caso d’uso.
        //coordinare mapping → publish.

        private readonly ITicketQueuePublisher _publisher;
        private readonly ITicketIngestedFactory _factory;
        private readonly ITicketStatusRepository _statusRepository;

        public TicketIngestPipeline(ITicketQueuePublisher publisher, ITicketIngestedFactory factory, ITicketStatusRepository statusRepository)
        {
            _publisher = publisher;
            _factory = factory;
            _statusRepository = statusRepository;
        }


        public async Task ExecuteAsync(TicketIngestedRequest request, string correlationId, CancellationToken ct = default)
        {
            var ticketEvent = _factory.Create(request, correlationId);

            await _statusRepository.PatchReceivedAsync(ticketEvent, ct);
            await _publisher.PublishAsync(ticketEvent, ct);

        }
    }
}
