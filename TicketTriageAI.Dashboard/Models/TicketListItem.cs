using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Dashboard.Models
{
    public sealed class TicketListItem
    {
        public string MessageId { get; init; } = default!;
        public DateTime ReceivedAt { get; init; }
        public string From { get; init; } = default!;
        public string Subject { get; init; } = default!;
        public string? Category { get; init; }
        public string? Severity { get; init; }
        public double Confidence { get; init; }
        public TicketStatus Status { get; init; }
        public string? StatusReason { get; init; }
    }
}
