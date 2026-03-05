using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Messaging;
using TicketTriageAI.Core.Services.Observability;
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
        private readonly ITicketTelemetry _telemetry;

        public TicketIngestPipeline(
            ITicketQueuePublisher publisher,
            ITicketIngestedFactory factory,
            ITicketStatusRepository statusRepository,
            ITicketTelemetry telemetry)
        {
            _publisher = publisher;
            _factory = factory;
            _statusRepository = statusRepository;
            _telemetry = telemetry;
        }


        public async Task<bool> ExecuteAsync(
            TicketIngestedRequest request,
            string correlationId,
            string? idempotencyKey = null,
            CancellationToken ct = default)
        {
            var messageId = !string.IsNullOrWhiteSpace(idempotencyKey)
                ? idempotencyKey.Trim()
                : ComputeDedupeKey(request, correlationId);
            if (await _statusRepository.ExistsAsync(messageId, ct))
            {
                return false;
            }

            var ticketEvent = _factory.Create(request, correlationId, messageId);

            await _statusRepository.PatchReceivedAsync(ticketEvent, ct);
            await _publisher.PublishAsync(ticketEvent, ct);

            _telemetry.TicketIngested(ticketEvent);

            return true;
        }
        private static string ComputeDedupeKey(TicketIngestedRequest request, string correlationId)
        {
            var received = request.ReceivedAt; 


            var canonical = string.Join("|",
                request.From?.Trim().ToLowerInvariant(),
                request.Subject?.Trim().ToLowerInvariant(),
                request.Body?.Trim(),
                request.Source?.Trim().ToLowerInvariant(),
                request.ReceivedAt.ToUniversalTime().ToString("O"));

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(canonical);
            var hash = sha.ComputeHash(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }


    }
}
