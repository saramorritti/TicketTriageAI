using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public interface ITicketClassifier
    {
        // Astrazione del componente di classificazione dei ticket.
        // Permette di sostituire facilmente implementazioni fake o AI reali senza cambiare la pipeline.

        Task<TicketTriageResult> ClassifyAsync(TicketIngested ticket, CancellationToken ct = default);
    }
}
