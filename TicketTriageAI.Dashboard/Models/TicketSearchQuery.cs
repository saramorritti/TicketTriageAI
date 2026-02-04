using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Dashboard.Models
{
    public sealed class TicketSearchQuery
    {
        public TicketStatus? Status { get; init; }
        public string? Q { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }
}
