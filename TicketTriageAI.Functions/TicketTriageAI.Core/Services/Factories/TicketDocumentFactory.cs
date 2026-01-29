using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Factories
{
    public sealed class TicketDocumentFactory : ITicketDocumentFactory
    {
        public TicketDocument Create(TicketIngested ticket, TicketTriageResult triage, ClassifierMetadata meta)
            => new()
            {
                Id = ticket.MessageId,
                MessageId = ticket.MessageId,

                CorrelationId = ticket.CorrelationId,
                From = ticket.From,
                Subject = ticket.Subject,
                Body = ticket.Body,
                ReceivedAt = ticket.ReceivedAt,
                Source = ticket.Source,

                Category = triage.Category,
                Severity = triage.Severity,
                Confidence = triage.Confidence,
                NeedsHumanReview = triage.NeedsHumanReview,

                RawMessage = ticket.RawMessage,
                ClassifierName = meta.Name,
                ClassifierVersion = meta.Version,
                Model = meta.Model,

                ProcessedAtUtc = DateTime.UtcNow
            };
    }
}
