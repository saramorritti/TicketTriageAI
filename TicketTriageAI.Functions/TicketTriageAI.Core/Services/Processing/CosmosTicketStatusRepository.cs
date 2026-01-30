using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class CosmosTicketStatusRepository : ITicketStatusRepository
    {
        private readonly Container _container;
        private readonly ITicketDocumentFactory _docFactory;

        public CosmosTicketStatusRepository(
        CosmosClient client,
        IOptions<CosmosOptions> options,
        ITicketDocumentFactory docFactory)
        {
            var opt = options.Value;
            _container = client.GetContainer(opt.DatabaseName, opt.ContainerName);
            _docFactory = docFactory;
        }


        public async Task PatchReceivedAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            if (ticket is null) throw new ArgumentNullException(nameof(ticket));

            var patch = new List<PatchOperation>
    {
        PatchOperation.Set("/messageId", ticket.MessageId),
        PatchOperation.Set("/correlationId", ticket.CorrelationId),
        PatchOperation.Set("/from", ticket.From),
        PatchOperation.Set("/subject", ticket.Subject),
        PatchOperation.Set("/body", ticket.Body),
        PatchOperation.Set("/receivedAt", ticket.ReceivedAt),
        PatchOperation.Set("/source", ticket.Source),
        PatchOperation.Set("/status", (int)TicketStatus.Received),
        PatchOperation.Remove("/statusReason")
    };

            try
            {
                await _container.PatchItemAsync<dynamic>(
                    id: ticket.MessageId,
                    partitionKey: new PartitionKey(ticket.MessageId),
                    patchOperations: patch,
                    cancellationToken: ct);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var doc = _docFactory.CreateReceived(ticket);

                await _container.CreateItemAsync(
                    item: doc,
                    partitionKey: new PartitionKey(ticket.MessageId),
                    cancellationToken: ct);
            }
        }
    }
}
