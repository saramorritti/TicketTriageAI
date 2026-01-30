using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Functions.Common;

namespace TicketTriageAI.Functions.Functions
{
    public sealed class ProcessTicketDlqFunction
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ILogger<ProcessTicketDlqFunction> _logger;
        private readonly ITicketRepository _repository;

        public ProcessTicketDlqFunction(
            ILogger<ProcessTicketDlqFunction> logger,
            ITicketRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [Function("ProcessTicketDlq")]
        public async Task RunAsync(
            // DLQ path: <queue> + "/$DeadLetterQueue"
            [ServiceBusTrigger("tickets-ingest/$DeadLetterQueue", Connection = "ServiceBusConnection")] string message,
            CancellationToken ct)
        {
            TicketIngested? ticket;

            try
            {
                ticket = JsonSerializer.Deserialize<TicketIngested>(message, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid DLQ payload (JSON). Raw={RawMessage}", message);
                return;
            }

            if (ticket is null)
            {
                _logger.LogError("Invalid DLQ payload (null deserialization). Raw={RawMessage}", message);
                return;
            }

            ticket.RawMessage = message;

            using (_logger.BeginCorrelationScope(ticket.CorrelationId, ticket.MessageId))
            {
                _logger.LogWarning("Message landed in DLQ. Marking as Failed. MessageId={MessageId}", ticket.MessageId);

                // Creiamo un doc minimo "failed" (upsert sullo stesso id/messageId)
                var failedDoc = new TicketDocument
                {
                    Id = ticket.MessageId,
                    MessageId = ticket.MessageId,
                    CorrelationId = ticket.CorrelationId,
                    From = ticket.From,
                    Subject = ticket.Subject,
                    Body = ticket.Body,
                    ReceivedAt = ticket.ReceivedAt,
                    Source = ticket.Source,
                    RawMessage = ticket.RawMessage,

                    Status = TicketStatus.Failed,
                    StatusReason = "DeadLetter",

                    ProcessedAtUtc = DateTime.UtcNow
                };

                await _repository.UpsertAsync(failedDoc, ct);

                _logger.LogWarning("DLQ message marked as Failed in Cosmos. MessageId={MessageId}", ticket.MessageId);
            }
        }
    }
}
