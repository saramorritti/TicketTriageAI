using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class FakeTicketClassifier : ITicketClassifier
    {
        public Task<TicketTriageResult> ClassifyAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            // Implementazione fittizia del classifier: simula il risultato di una classificazione AI.
            // Usata per testare pipeline e flusso senza dipendere da servizi AI reali.
            var confidence = ticket.Subject.Contains("urgente", StringComparison.OrdinalIgnoreCase) ? 0.90 : 0.55;
            var severity = confidence >= 0.80 ? "P1" : "P3";

            return Task.FromResult(new TicketTriageResult
            {
                Category = "support",
                Severity = severity,
                Confidence = confidence,
                NeedsHumanReview = confidence < 0.65
            });
        }
    }
}
