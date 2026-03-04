using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public sealed class TicketNotificationMessage
    {
        public string MessageId { get; init; } = default!;
        public string? CorrelationId { get; init; }
        public string? From { get; init; }
        public string? Subject { get; init; }
        public string? Category { get; init; }
        public string? Severity { get; init; }
        public double Confidence { get; init; }
        public string? StatusReason { get; init; }
        public string DashboardLink { get; init; } = default!;
        public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    }
}
