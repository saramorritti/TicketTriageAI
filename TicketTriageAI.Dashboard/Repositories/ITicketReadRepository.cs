using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Models;

namespace TicketTriageAI.Dashboard.Repositories
{
    public interface ITicketReadRepository
    {
        Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchQuery query, CancellationToken ct = default);
        Task<TicketDocument?> GetAsync(string messageId, CancellationToken ct = default);
    }
}
