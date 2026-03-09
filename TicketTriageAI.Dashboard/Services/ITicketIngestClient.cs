using TicketTriageAI.Dashboard.Models;

namespace TicketTriageAI.Dashboard.Services
{
    public interface ITicketIngestClient
    {
        Task<IngestCallResult> CreateAsync(CreateTicketInput input, string messageId, CancellationToken ct = default);
    }
}
