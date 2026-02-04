using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Models;

namespace TicketTriageAI.Dashboard.Repositories
{
    public interface ITicketReadRepository
    {
        Task<PagedResult<TicketListItem>> SearchAsync(TicketSearchQuery query, string? continuationToken, CancellationToken ct = default);
        Task<TicketDocument?> GetAsync(string messageId, CancellationToken ct = default);
    }
}
