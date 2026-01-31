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
        PatchOperation.Set("/statusReason", (string?)null)
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
        public Task PatchProcessingAsync(string messageId, CancellationToken ct = default)
    => PatchStatusAsync(messageId, TicketStatus.Processing, reason: null, ct);

        public Task PatchProcessedAsync(string messageId, CancellationToken ct = default)
            => PatchStatusAsync(messageId, TicketStatus.Processed, reason: null, ct);

        public Task PatchNeedsReviewAsync(string messageId, string reason, CancellationToken ct = default)
            => PatchStatusAsync(messageId, TicketStatus.NeedsReview, reason, ct);

        public Task PatchFailedAsync(string messageId, string reason, CancellationToken ct = default)
            => PatchStatusAsync(messageId, TicketStatus.Failed, reason, ct);

        private async Task PatchStatusAsync(
    string messageId,
    TicketStatus status,
    string? reason,
    CancellationToken ct)
        {
            var ops = new List<PatchOperation>
    {
        PatchOperation.Set("/status", (int)status)
    };

            ops.Add(PatchOperation.Set("/statusReason",
                string.IsNullOrWhiteSpace(reason) ? null : reason));

            await _container.PatchItemAsync<dynamic>(
                id: messageId,
                partitionKey: new PartitionKey(messageId),
                patchOperations: ops,
                cancellationToken: ct);
        }

    }
}
