using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Models;

namespace TicketTriageAI.Dashboard.Repositories
{
    internal sealed class TicketListRow
    {
        [JsonProperty("messageId")]
        public string MessageId { get; set; } = default!;

        [JsonProperty("receivedAt")]
        public DateTime? ReceivedAt { get; set; }
        [JsonProperty("sender")]
        public string Sender { get; set; } = default!;

        [JsonProperty("subject")]
        public string Subject { get; set; } = default!;

        [JsonProperty("category")]
        public string? Category { get; set; }

        [JsonProperty("severity")]
        public string? Severity { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("statusReason")]
        public string? StatusReason { get; set; }
    }

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
            if (query is null) throw new ArgumentNullException(nameof(query));
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

            using var it = _container.GetItemQueryIterator<TicketListRow>(qd, continuationToken, options);

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
                var received = row.ReceivedAt.HasValue ? NormalizeUtc(row.ReceivedAt.Value) : DateTime.MinValue;
                results.Add(new TicketListItem
                {
                    MessageId = row.MessageId,
                    ReceivedAt = received,
                    From = row.Sender,
                    Subject = row.Subject,
                    Category = row.Category,
                    Severity = row.Severity,
                    Confidence = row.Confidence,
                    Status = (TicketStatus)row.Status,
                    StatusReason = row.StatusReason
                });
            }

            return new PagedResult<TicketListItem>
            {
                Items = results,
                ContinuationToken = pageRes.ContinuationToken
            };
        }

        private static DateTime NormalizeUtc(DateTime dt)
        {
            // Cosmos spesso deserializza DateTime con Kind=Unspecified.
            if (dt.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            return dt.ToUniversalTime();
        }
    }
}
