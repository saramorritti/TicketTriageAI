using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Factories
{
    public sealed class TicketIngestedFactory : ITicketIngestedFactory
    {
        public TicketIngested Create(TicketIngestedRequest request, string correlationId, string? rawMessage = null)
            => new()
            {
                MessageId = request.MessageId,
                CorrelationId = correlationId,
                From = request.From,
                Subject = request.Subject,
                Body = request.Body,
                ReceivedAt = request.ReceivedAt,
                Source = request.Source,
                RawMessage = rawMessage
            };
    }
}
