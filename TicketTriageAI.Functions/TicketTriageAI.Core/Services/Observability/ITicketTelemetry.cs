using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Observability
{
    public interface ITicketTelemetry
    {
        void TicketIngested(TicketIngested ticket);
        void TicketProcessed(TicketDocument doc);
        void TicketNeedsReview(TicketDocument doc);
        void TicketFailed(string correlationId, string messageId, string reason, Exception? ex = null);
    }
}
