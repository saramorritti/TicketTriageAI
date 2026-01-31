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
    public sealed class CosmosTicketRepository : ITicketRepository
    {
        // Implementazione del repository che salva i ticket su Cosmos DB
        // Gestisce l’upsert del documento usando messageId come partition key

        private readonly Container _container;

        public CosmosTicketRepository(
        CosmosClient client,
        IOptions<CosmosOptions> options)
        {
            var opt = options.Value;
            _container = client.GetContainer(opt.DatabaseName, opt.ContainerName);
        }


        public Task UpsertAsync(TicketDocument doc, CancellationToken ct = default)
        {
            return _container.UpsertItemAsync(
                item: doc,
                partitionKey: new PartitionKey(doc.MessageId),
                cancellationToken: ct);
        }
    }
}
