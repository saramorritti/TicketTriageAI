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
            [ServiceBusTrigger("tickets-ingest", Connection = "ServiceBusConnection")] string message)
        {
            var ticket = JsonSerializer.Deserialize<TicketIngested>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (ticket is null)
            {
                _logger.LogError("Invalid message payload.");
                return;
            }

            using (_logger.BeginCorrelationScope(ticket.CorrelationId))
            {
                _logger.LogInformation(
                    "Processing ticket. MessageId: {MessageId} Subject: {Subject}",
                    ticket.MessageId,
                    ticket.Subject);

                await _pipeline.ExecuteAsync(ticket);

                _logger.LogInformation("Processed OK.");
            }
        }
    }
}
