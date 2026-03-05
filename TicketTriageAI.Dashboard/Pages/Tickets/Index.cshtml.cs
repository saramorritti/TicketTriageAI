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
        public int PageSize { get; set; } = 25;

        public async Task OnGetAsync(string? q, TicketStatus? status, int pageSize = 25, int page = 1, string? continuationToken = null)
        {
            Q = q;
            Status = status;
            ContinuationToken = continuationToken;

            Page = Math.Max(1, page);
            PageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _repo.SearchAsync(
                new TicketSearchQuery
                {
                    Q = Q,
                    Status = Status,
                    PageSize = PageSize
                },
                ContinuationToken);

            Items = result.Items;
            NextContinuationToken = result.ContinuationToken;
        }

    }
}
