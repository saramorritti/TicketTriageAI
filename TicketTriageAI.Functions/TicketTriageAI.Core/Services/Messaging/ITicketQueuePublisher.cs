using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Messaging
{
    public interface ITicketQueuePublisher
    {
        // Astrazione di pubblicazione su coda (messaging boundary).
        // Serve a disaccoppiare la pipeline dal trasporto (Service Bus) per testabilità e sostituibilità.

        Task PublishAsync(TicketIngested ticket, CancellationToken ct = default);
    }
}
