using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public sealed class TicketIngestedRequest
    {
        public string MessageId { get; init; } = default!;
        public string From { get; init; } = default!;
        public string Subject { get; init; } = default!;
        public string Body { get; init; } = default!;
        public DateTime ReceivedAt { get; init; }

        // opzionale: default utile
        public string Source { get; init; } = "email";
    }
}
