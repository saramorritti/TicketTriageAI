using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public sealed class TicketIngested
    {
        // Evento interno pubblicato su Service Bus: rappresenta un ticket “accettato” dal sistema.
        // È un contratto stabile, completo e tracciabile (CorrelationId) indipendente dal canale di ingresso.

        public string MessageId { get; init; } = default!;
        public string CorrelationId { get; init; } = default!;
        public string From { get; init; } = default!;
        public string Subject { get; init; } = default!;
        public string Body { get; init; } = default!;
        public DateTime ReceivedAt { get; init; }
        public string Source { get; init; } = "email";
        public string RawMessage { get; set; } = string.Empty;

    }
}
