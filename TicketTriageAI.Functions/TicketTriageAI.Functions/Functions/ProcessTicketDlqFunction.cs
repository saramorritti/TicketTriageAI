using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TicketTriageAI.Common.Logging;
using TicketTriageAI.Common.Serialization;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Processing;

namespace TicketTriageAI.Functions.Functions
{
    public sealed class ProcessTicketDlqFunction
    {
        private readonly ILogger<ProcessTicketDlqFunction> _logger;
        private readonly ITicketRepository _repository;
        private readonly ITicketDocumentFactory _docFactory;

        public ProcessTicketDlqFunction(
            ILogger<ProcessTicketDlqFunction> logger,
            ITicketRepository repository,
            ITicketDocumentFactory docFactory)
        {
            _logger = logger;
            _repository = repository;
            _docFactory = docFactory;
        }

        [Function("ProcessTicketDlq")]
        public async Task RunAsync(
        [ServiceBusTrigger("tickets-ingest/$DeadLetterQueue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage sbMessage,
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
                _logger.LogError(ex, "Invalid DLQ payload (JSON). Raw={Raw}", SafeLog.SafePayload(message));
                return;
            }

            if (ticket is null)
            {
                _logger.LogError("Invalid DLQ payload (null deserialization). Raw={Raw}", SafeLog.SafePayload(message));
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

            ticket.RawMessage = message;

            using (_logger.BeginCorrelationScope(ticket.CorrelationId, ticket.MessageId))
            {
                _logger.LogWarning(
                    "Message landed in DLQ. Marking as Failed. MessageId={MessageId} CorrelationId={CorrelationId}",
                    ticket.MessageId,
                    ticket.CorrelationId);

                var failedDoc = _docFactory.CreateFailedFromDlq(ticket, TicketStatusReason.DeadLetter);
                await _repository.UpsertAsync(failedDoc, ct);

                _logger.LogWarning("DLQ message marked as Failed in Cosmos. MessageId={MessageId}", ticket.MessageId);
            }
        }
    }
}