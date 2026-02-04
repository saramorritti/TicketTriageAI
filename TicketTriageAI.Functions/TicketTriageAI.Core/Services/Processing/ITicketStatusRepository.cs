using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public interface ITicketStatusRepository
    {
        Task PatchReceivedAsync(TicketIngested ticket, CancellationToken ct = default);

        Task PatchProcessingAsync(string messageId, CancellationToken ct = default);
        Task PatchProcessedAsync(string messageId, CancellationToken ct = default);
        Task PatchNeedsReviewAsync(string messageId, string reason, CancellationToken ct = default);
        Task PatchFailedAsync(string messageId, string reason, CancellationToken ct = default);
        Task<bool> ExistsAsync(string messageId, CancellationToken ct = default);


    }
}
