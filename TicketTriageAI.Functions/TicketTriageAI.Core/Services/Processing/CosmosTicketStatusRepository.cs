using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class CosmosTicketStatusRepository : ITicketStatusRepository
    {
        private readonly Container _container;

        public CosmosTicketStatusRepository(
        CosmosClient client,
        IOptions<CosmosOptions> options)
        {
            var opt = options.Value;
            _container = client.GetContainer(opt.DatabaseName, opt.ContainerName);
        }

        public Task UpsertReceivedAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            if (ticket is null) throw new ArgumentNullException(nameof(ticket));

            // Upsert "minimo" per tracciare Received
            var doc = new
            {
                id = ticket.MessageId,
                messageId = ticket.MessageId,
                CorrelationId = ticket.CorrelationId,
                From = ticket.From,
                Subject = ticket.Subject,
                Body = ticket.Body,
                ReceivedAt = ticket.ReceivedAt,
                Source = ticket.Source,
                Status = (int)TicketStatus.Received,
                StatusReason = (string?)null
            };

            return _container.UpsertItemAsync(doc, new PartitionKey(ticket.MessageId), cancellationToken: ct);
        }
    }
}
