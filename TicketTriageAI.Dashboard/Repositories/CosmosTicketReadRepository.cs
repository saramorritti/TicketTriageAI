using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Models;

namespace TicketTriageAI.Dashboard.Repositories
{
    public sealed class CosmosTicketReadRepository : ITicketReadRepository
    {
        private readonly Container _container;

        public CosmosTicketReadRepository(CosmosClient client, IOptions<CosmosOptions> options)
        {
            var opt = options.Value;
            _container = client.GetContainer(opt.DatabaseName, opt.ContainerName);
        }

        public async Task<TicketDocument?> GetAsync(string messageId, CancellationToken ct = default)
        {
            try
            {
                var res = await _container.ReadItemAsync<TicketDocument>(
                    id: messageId,
                    partitionKey: new PartitionKey(messageId),
                    cancellationToken: ct);

                return res.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<PagedResult<TicketListItem>> SearchAsync(
    TicketSearchQuery query,
    string? continuationToken,
    CancellationToken ct = default)
        {
            var size = Math.Clamp(query.PageSize, 1, 100);

            var where = "WHERE 1=1";

            if (query.Status is not null)
                where += " AND c.status = @status";

            if (!string.IsNullOrWhiteSpace(query.Q))
                where += " AND (CONTAINS(LOWER(c.subject), @q) OR CONTAINS(LOWER(c[\"from\"]), @q))";

            var sql =
                $@"SELECT 
              c.messageId,
              c.receivedAt,
              c[""from""] AS sender,
              c.subject,
              c.category,
              c.severity,
              c.confidence,
              c.status,
              c.statusReason
           FROM c
           {where}
           ORDER BY c.receivedAt DESC";

            var qd = new QueryDefinition(sql);

            if (query.Status is not null)
                qd = qd.WithParameter("@status", (int)query.Status.Value);

            if (!string.IsNullOrWhiteSpace(query.Q))
                qd = qd.WithParameter("@q", query.Q.Trim().ToLowerInvariant());

            var options = new QueryRequestOptions
            {
                MaxItemCount = size
            };

            using var it = _container.GetItemQueryIterator<dynamic>(
                qd,
                continuationToken,
                options);

            var results = new List<TicketListItem>();

            if (!it.HasMoreResults)
            {
                return new PagedResult<TicketListItem>
                {
                    Items = results,
                    ContinuationToken = null
                };
            }

            var pageRes = await it.ReadNextAsync(ct);

            foreach (var row in pageRes)
            {
                results.Add(new TicketListItem
                {
                    MessageId = row.messageId,
                    ReceivedAt = row.receivedAt,
                    From = row.sender,
                    Subject = row.subject,
                    Category = row.category,
                    Severity = row.severity,
                    Confidence = row.confidence,
                    Status = (TicketStatus)(int)row.status,
                    StatusReason = row.statusReason
                });
            }

            return new PagedResult<TicketListItem>
            {
                Items = results,
                ContinuationToken = pageRes.ContinuationToken
            };
        }


    }
}
