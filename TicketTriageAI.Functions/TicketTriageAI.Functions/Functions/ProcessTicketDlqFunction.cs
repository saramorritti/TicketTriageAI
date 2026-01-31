using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Common.Logging;
using TicketTriageAI.Common.Serialization;

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
            // DLQ path: <queue> + "/$DeadLetterQueue"
            [ServiceBusTrigger("tickets-ingest/$DeadLetterQueue", Connection = "ServiceBusConnection")] string message,
            CancellationToken ct)
        {
            TicketIngested? ticket;

            try
            {
                ticket = JsonSerializer.Deserialize<TicketIngested>(message, JsonDefaults.Options);
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

                var failedDoc = _docFactory.CreateFailedFromDlq(ticket, TicketStatusReason.DeadLetter);
                await _repository.UpsertAsync(failedDoc, ct);

                _logger.LogWarning("DLQ message marked as Failed in Cosmos. MessageId={MessageId}", ticket.MessageId);
            }
        }
    }
}
