using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Models;
using TicketTriageAI.Dashboard.Repositories;

namespace TicketTriageAI.Dashboard.Pages.Tickets
{
    public sealed class IndexModel : PageModel
    {
        private readonly ITicketReadRepository _repo;

        public IndexModel(ITicketReadRepository repo) => _repo = repo;

        public IReadOnlyList<TicketListItem> Items { get; private set; } = Array.Empty<TicketListItem>();

        public string? Q { get; set; }
        public TicketStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public string? ContinuationToken { get; set; }
        public string? NextContinuationToken { get; private set; }


        public async Task OnGetAsync(string? q, TicketStatus? status, string? continuationToken = null)
        {
            Q = q;
            Status = status;
            ContinuationToken = continuationToken;

            var result = await _repo.SearchAsync(
                new TicketSearchQuery
                {
                    Q = Q,
                    Status = Status,
                    PageSize = 25
                },
                ContinuationToken);

            Items = result.Items;
            NextContinuationToken = result.ContinuationToken;
        }

    }
}
