using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public sealed class TicketTriageResult
    {
        public string Category { get; init; } = "other";
        public string Severity { get; init; } = "P3";
        public double Confidence { get; init; }
        public bool NeedsHumanReview { get; init; }
        public string Summary { get; init; } = string.Empty;
        public IReadOnlyList<string> Entities { get; init; } = Array.Empty<string>();

        public static TicketTriageResult Create(
            string category,
            string severity,
            double confidence,
            bool needsHumanReview,
            string summary,
            IReadOnlyList<string>? entities)
            => new()
            {
                Category = category,
                Severity = severity,
                Confidence = confidence,
                NeedsHumanReview = needsHumanReview,
                Summary = summary ?? string.Empty,
                Entities = entities ?? Array.Empty<string>()
            };

        public static TicketTriageResult Fallback(string category = "other", string severity = "P3", string? reason = null)
            => new()
            {
                Category = category,
                Severity = severity,
                Confidence = 0.0,
                NeedsHumanReview = true,
                Summary = string.Empty,
                Entities = Array.Empty<string>()
            };
    }
}
