using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Text;

namespace TicketTriageAI.Core.Services.Factories
{
    public sealed class TicketNormalizationFactory : ITicketNormalizationFactory
    {
        private readonly ITextNormalizer _normalizer;

        public TicketNormalizationFactory(ITextNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        public (TicketIngested Normalized, string CleanBody) CreateNormalized(TicketIngested input)
        {
            var cleanedBody = _normalizer.Normalize(input.Body ?? string.Empty);

            var normalized = new TicketIngested
            {
                MessageId = input.MessageId,
                CorrelationId = input.CorrelationId,
                From = input.From,
                Subject = input.Subject,
                Body = cleanedBody,
                ReceivedAt = input.ReceivedAt,
                Source = input.Source,
                RawMessage = input.RawMessage
            };

            return (normalized, cleanedBody);
        }
    }
}
