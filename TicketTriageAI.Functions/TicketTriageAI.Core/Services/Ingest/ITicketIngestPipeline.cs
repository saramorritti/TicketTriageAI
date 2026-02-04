using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Ingest
{
    public interface ITicketIngestPipeline
    {
        Task<bool> ExecuteAsync(TicketIngestedRequest request, string correlationId, CancellationToken ct = default);
    }
}
