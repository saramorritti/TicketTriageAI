using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public sealed class TicketTriageResult
    {
        public string Category { get; init; } = "unknown";
        public string Severity { get; init; } = "P3";
        public double Confidence { get; init; }
        public bool NeedsHumanReview { get; init; }
    }
}
