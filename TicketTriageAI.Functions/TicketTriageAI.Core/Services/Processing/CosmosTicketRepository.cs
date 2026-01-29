using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class CosmosTicketRepository : ITicketRepository
    {
        // Implementazione del repository che salva i ticket su Cosmos DB
        // Gestisce l’upsert del documento usando messageId come partition key

        private readonly Container _container;

        public CosmosTicketRepository(CosmosClient client)
        {
            // DEVONO corrispondere a come hai creato Cosmos nel portal
            _container = client.GetContainer("TicketTriage", "Tickets");
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
