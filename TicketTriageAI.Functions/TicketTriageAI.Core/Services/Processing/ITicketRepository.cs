using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public interface ITicketRepository
    {
        Task UpsertAsync(TicketDocument doc, CancellationToken ct = default);
    }
}
