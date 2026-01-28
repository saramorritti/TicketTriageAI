using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Interfaces;

namespace TicketTriageAI.Core.Services
{
    public sealed class TicketIngestPipeline : ITicketIngestPipeline
    {
        private readonly ITicketQueuePublisher _publisher;

        public TicketIngestPipeline(ITicketQueuePublisher publisher)
        {
            _publisher = publisher;
        }

        public Task ExecuteAsync(
            TicketIngestedRequest request,
            string correlationId,
            CancellationToken ct = default)
        {
            var ticketEvent = new TicketIngested
            {
                MessageId = request.MessageId,
                CorrelationId = correlationId,
                From = request.From,
                Subject = request.Subject,
                Body = request.Body,
                ReceivedAt = request.ReceivedAt,
                Source = request.Source
            };

            return _publisher.PublishAsync(ticketEvent, ct);
        }
    }

}
