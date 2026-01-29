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
    public sealed class ProcessTicketFunction
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
            [ServiceBusTrigger("tickets-ingest", Connection = "ServiceBusConnection")] string message,
            CancellationToken ct)
        {
            TicketIngested? ticket;

            try
            {
                ticket = JsonSerializer.Deserialize<TicketIngested>(message, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid message payload (JSON). Raw={RawMessage}", message);
                return;
            }

            if (ticket is null)
            {
                _logger.LogError("Invalid message payload (null deserialization). Raw={RawMessage}", message);
                return;
            }

            // audit: conserva sempre il raw
            ticket.RawMessage = message;

            using (_logger.BeginCorrelationScope(ticket.CorrelationId, ticket.MessageId))
            {
                _logger.LogInformation(
                    "Processing ticket. Subject: {Subject}",
                    ticket.Subject);

                await _pipeline.ExecuteAsync(ticket, ct);

                _logger.LogInformation("Processed OK.");
            }
        }
    }
}
