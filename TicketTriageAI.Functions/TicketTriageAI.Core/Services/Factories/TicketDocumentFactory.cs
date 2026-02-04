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
                Summary = triage.Summary,
                Entities = triage.Entities?.ToArray(),

                RawMessage = ticket.RawMessage,
                ClassifierName = meta.Name,
                ClassifierVersion = meta.Version,
                Model = meta.Model,

                ProcessedAtUtc = DateTime.UtcNow
            };
        public TicketDocument CreateReceived(TicketIngested ticket)
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

                // campi "triage" non disponibili in ingest -> default safe
                Category = "n/a",
                Severity = "n/a",
                Confidence = 0,
                NeedsHumanReview = false,

                Summary = string.Empty,
                Entities = Array.Empty<string>(),

                RawMessage = ticket.RawMessage ?? string.Empty,
                ClassifierName = "system",
                ClassifierVersion = "n/a",
                Model = null,

                Status = TicketStatus.Received,
                StatusReason = null,
                ProcessedAtUtc = DateTime.UtcNow
            };

        public TicketDocument CreateFailedFromDlq(TicketIngested ticket, string reason)
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

                Category = "dlq",
                Severity = "n/a",
                Confidence = 0,
                NeedsHumanReview = true,
                Summary = string.Empty,
                Entities = Array.Empty<string>(),

                RawMessage = ticket.RawMessage ?? string.Empty,
                ClassifierName = "system",
                ClassifierVersion = "n/a",
                Model = null,

                Status = TicketStatus.Failed,
                StatusReason = reason,
                ProcessedAtUtc = DateTime.UtcNow
            };

    }
}
