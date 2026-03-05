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
using TicketTriageAI.Common.Logging;
using TicketTriageAI.Common.Serialization;
using Azure.Messaging.ServiceBus;

namespace TicketTriageAI.Functions.Functions
{
    public sealed class ProcessTicketFunction
    {
        private readonly ILogger<ProcessTicketFunction> _logger;
        private readonly ITicketProcessingPipeline _pipeline;

        public ProcessTicketFunction(
            ILogger<ProcessTicketFunction> logger,
            ITicketProcessingPipeline pipeline)
        {
            _logger = logger;
            _pipeline = pipeline;
        }

        [Function("ProcessTicket")]
        public async Task RunAsync(
        [ServiceBusTrigger("tickets-ingest", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage sbMessage,
        FunctionContext context,
        CancellationToken ct)
        {
            var message = sbMessage.Body.ToString(); // JSON
            TicketIngested? ticket;

            try
            {
                ticket = JsonSerializer.Deserialize<TicketIngested>(message, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid message payload (JSON). Raw={Raw}", SafeLog.SafePayload(message));
                return;
            }

            if (ticket is null)
            {
                _logger.LogError("Invalid message payload (null deserialization). Raw={Raw}", SafeLog.SafePayload(message));
                return;
            }

            // Ensure correlation/message id sempre valorizzati (init-only -> uso with)
            var ensuredMessageId =
                string.IsNullOrWhiteSpace(ticket.MessageId) ? (sbMessage.MessageId ?? ticket.MessageId) : ticket.MessageId;

            var ensuredCorrelationId =
                string.IsNullOrWhiteSpace(ticket.CorrelationId) ? (sbMessage.CorrelationId ?? context.InvocationId) : ticket.CorrelationId;

            ticket = ticket with
            {
                MessageId = ensuredMessageId,
                CorrelationId = ensuredCorrelationId
            };
            // ReceivedAt = momento in cui il messaggio è stato enqueued su Service Bus
            var receivedAtUtc = sbMessage.EnqueuedTime.UtcDateTime;
            if (receivedAtUtc == default)
                receivedAtUtc = DateTime.UtcNow;
            _logger.LogInformation("ReceivedAt(from SB)={ReceivedAt:o}", ticket.ReceivedAt);
            // TicketIngested è record init-only -> uso with
            ticket = ticket with
            {
                ReceivedAt = receivedAtUtc
            };
            // RawMessage può restare settable
            ticket.RawMessage = message;

            using (_logger.BeginCorrelationScope(ticket.CorrelationId, ticket.MessageId))
            {
                _logger.LogInformation("Processing ticket. Subject: {Subject}", ticket.Subject);
                await _pipeline.ExecuteAsync(ticket, ct);
                _logger.LogInformation("Processed OK.");
            }
        }
    }
}
