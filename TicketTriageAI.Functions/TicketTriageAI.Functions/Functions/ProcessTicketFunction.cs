using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Functions.Functions
{
    public sealed class ProcessTicketFunction
    {
        private readonly ILogger<ProcessTicketFunction> _logger;

        public ProcessTicketFunction(ILogger<ProcessTicketFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessTicket")]
        public Task RunAsync(
            [ServiceBusTrigger("tickets-ingest", Connection = "ServiceBusConnection")] string message)
        {
            var ticket = JsonSerializer.Deserialize<TicketIngested>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (ticket is null)
            {
                _logger.LogError("Invalid message payload.");
                return Task.CompletedTask;
            }

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = ticket.CorrelationId
            }))
            {
                _logger.LogInformation(
                    "Processing ticket. MessageId: {MessageId} Subject: {Subject}",
                    ticket.MessageId,
                    ticket.Subject);

                // MVP: per ora solo log. Dopo aggiungiamo AI + persistenza.
                _logger.LogInformation("Processed OK.");
            }

            return Task.CompletedTask;
        }
    }
}
