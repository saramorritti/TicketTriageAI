using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Models;
using TicketTriageAI.Dashboard.Options;
using TicketTriageAI.Dashboard.Repositories;
using TicketTriageAI.Dashboard.Services;

namespace TicketTriageAI.Dashboard.Pages.Tickets
{
    public sealed class IndexModel : PageModel
    {
        private readonly ITicketReadRepository _repo;
        private readonly ITicketIngestClient _ingestClient;
        private readonly IngestApiOptions _ingestOptions;

        public IndexModel(
            ITicketReadRepository repo,
            ITicketIngestClient ingestClient,
            IOptions<IngestApiOptions> ingestOptions)
        {
            _repo = repo;
            _ingestClient = ingestClient;
            _ingestOptions = ingestOptions.Value;
        }

        public IReadOnlyList<TicketListItem> Items { get; private set; } = Array.Empty<TicketListItem>();

        [BindProperty]
        public CreateTicketInput Input { get; set; } = new()
        {
            ReceivedAt = DateTime.UtcNow,
            Source = "email"
        };

        public string? Q { get; set; }
        public TicketStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public string? ContinuationToken { get; set; }
        public string? NextContinuationToken { get; private set; }
        public int PageSize { get; set; } = 25;

        public IngestCallResult? CreateResult { get; private set; }
        public TicketDocument? CreatedTicket { get; private set; }
        public string? CreatedMessageId { get; private set; }

        public async Task OnGetAsync(string? q, TicketStatus? status, int pageSize = 25, int pageNumber = 1, string? continuationToken = null)
        {
            await LoadListAsync(q, status, pageSize, pageNumber, continuationToken);
        }

        public async Task<IActionResult> OnPostCreateAsync(string? q, TicketStatus? status, int pageSize = 25, int pageNumber = 1, string? continuationToken = null, CancellationToken ct = default)
        {
            await LoadListAsync(q, status, pageSize, pageNumber, continuationToken);

            if (!ModelState.IsValid)
                return Page();

            var messageId = Guid.NewGuid().ToString("N");
            CreatedMessageId = messageId;

            CreateResult = await _ingestClient.CreateAsync(Input, messageId, ct);

            if (CreateResult.IsSuccess)
            {
                CreatedTicket = await WaitForTicketAsync(messageId, ct);
            }

            return Page();
        }

        private async Task LoadListAsync(string? q, TicketStatus? status, int pageSize, int pageNumber, string? continuationToken)
        {
            Q = q;
            Status = status;
            ContinuationToken = continuationToken;
            Page = Math.Max(1, pageNumber);
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

        private async Task<TicketDocument?> WaitForTicketAsync(string messageId, CancellationToken ct)
        {
            for (int i = 0; i < _ingestOptions.PollAttempts; i++)
            {
                var ticket = await _repo.GetAsync(messageId, ct);

                if (ticket is not null &&
                    (ticket.Status == TicketStatus.Processed ||
                     ticket.Status == TicketStatus.NeedsReview ||
                     ticket.Status == TicketStatus.Failed))
                {
                    return ticket;
                }

                await Task.Delay(_ingestOptions.PollDelayMilliseconds, ct);
            }

            return await _repo.GetAsync(messageId, ct);
        }
    }
}