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

        public IReadOnlyList<SampleTicketOption> SampleTickets { get; private set; } = Array.Empty<SampleTicketOption>();

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
            SampleTickets = BuildSampleTickets();
            await LoadListAsync(q, status, pageSize, pageNumber, continuationToken);
        }

        public async Task<IActionResult> OnPostCreateAsync(string? q, TicketStatus? status, int pageSize = 25, int pageNumber = 1, string? continuationToken = null, CancellationToken ct = default)
        {
            SampleTickets = BuildSampleTickets();
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

        public async Task<IActionResult> OnPostCreateSampleAsync(
            string sampleKey,
            string? q,
            TicketStatus? status,
            int pageSize = 25,
            int pageNumber = 1,
            string? continuationToken = null,
            CancellationToken ct = default)
        {
            SampleTickets = BuildSampleTickets();
            await LoadListAsync(q, status, pageSize, pageNumber, continuationToken);

            var sample = SampleTickets.FirstOrDefault(x =>
                string.Equals(x.Key, sampleKey, StringComparison.OrdinalIgnoreCase));

            if (sample is null)
            {
                ModelState.AddModelError(string.Empty, "Sample ticket non trovato.");
                return Page();
            }

            Input = new CreateTicketInput
            {
                From = sample.Payload.From,
                Subject = sample.Payload.Subject,
                Body = sample.Payload.Body,
                ReceivedAt = DateTime.UtcNow,
                Source = sample.Payload.Source
            };

            var messageId = Guid.NewGuid().ToString("N");
            CreatedMessageId = messageId;

            CreateResult = await _ingestClient.CreateAsync(Input, messageId, ct);

            if (CreateResult.IsSuccess)
            {
                CreatedTicket = await WaitForTicketAsync(messageId, ct);
            }

            return Page();
        }

        public IActionResult OnPostLoadSample(string sampleKey)
        {
            SampleTickets = BuildSampleTickets();

            var sample = SampleTickets.FirstOrDefault(x =>
                string.Equals(x.Key, sampleKey, StringComparison.OrdinalIgnoreCase));

            if (sample is null)
            {
                ModelState.AddModelError(string.Empty, "Sample ticket non trovato.");
                return Page();
            }

            Input = new CreateTicketInput
            {
                From = sample.Payload.From,
                Subject = sample.Payload.Subject,
                Body = sample.Payload.Body,
                ReceivedAt = DateTime.UtcNow,
                Source = sample.Payload.Source
            };

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

        private static IReadOnlyList<SampleTicketOption> BuildSampleTickets()
        {
            return new List<SampleTicketOption>
            {
                new()
                {
                    Key = "billing-p3",
                    Title = "Billing issue - duplicate charge",
                    ExpectedCategory = "billing",
                    ExpectedSeverity = "P3",
                    ExpectedOutcome = "Processed",
                    Description = "Problema amministrativo non bloccante, utile per mostrare un caso standard.",
                    Payload = new CreateTicketInput
                    {
                        From = "maria.rossi@contoso-demo.com",
                        Subject = "Duplicate charge on March invoice",
                        Body = """
                               Hello,
                               I noticed that my company card was charged twice for invoice INV-2026-0310.
                               The first charge appears correct, but a second identical charge was posted a few minutes later.
                               The service is still active and this is not blocking our operations, but we need clarification and a refund.
                               Customer: Contoso Demo
                               Invoice: INV-2026-0310
                               Amount: 248 EUR
                               """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                },
                new()
                {
                    Key = "support-p2",
                    Title = "Support request - account access problem",
                    ExpectedCategory = "support",
                    ExpectedSeverity = "P2",
                    ExpectedOutcome = "Processed",
                    Description = "Caso intermedio, impatta l’utente ma non l’intera piattaforma.",
                    Payload = new CreateTicketInput
                    {
                        From = "luca.bianchi@fabrikam-demo.com",
                        Subject = "Unable to access admin area after password reset",
                        Body = """
                               Hi team,
                               after resetting my password I can log into the main portal, but I still cannot access the admin area.
                               I receive a message saying I do not have sufficient permissions.
                               This blocks me from updating customer orders today.
                               User: luca.bianchi@fabrikam-demo.com
                               Department: Operations
                               Started this morning around 08:30 CET
                               """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                },
                new()
                {
                    Key = "technical-p1",
                    Title = "Technical incident - production API down",
                    ExpectedCategory = "technical",
                    ExpectedSeverity = "P1",
                    ExpectedOutcome = "NeedsReview",
                    Description = "Incidente critico, pensato per far scattare review umana.",
                    Payload = new CreateTicketInput
                    {
                        From = "oncall@northwind-demo.com",
                        Subject = "URGENT - Production API unavailable for all customers",
                        Body = """
                               Critical incident.
                               Since 09:05 CET our production API returns HTTP 500 on all checkout requests.
                               Multiple customers reported that orders cannot be completed.
                               This is affecting the whole production environment and revenue is impacted right now.
                               Error spike visible on payment and order endpoints.
                               Please escalate immediately.
                               """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                },
                new()
                {
                    Key = "other-review",
                    Title = "Ambiguous case - suspicious email and possible security concern",
                    ExpectedCategory = "other / support",
                    ExpectedSeverity = "P2",
                    ExpectedOutcome = "NeedsReview",
                    Description = "Caso volutamente ambiguo per mostrare bassa confidence o revisione manuale.",
                    Payload = new CreateTicketInput
                    {
                        From = "helpdesk@adatum-demo.com",
                        Subject = "Possible phishing email received by finance team",
                        Body = """
                               Hello,
                               our finance team received a suspicious email asking to change bank details for a supplier payment.
                               We are not sure whether this should be handled as a technical issue, a support request, or something else.
                               No systems are currently down, but the request looks risky and we prefer a manual review before proceeding.
                               Please advise on next steps.
                               """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                }
            };
        }
    }
}