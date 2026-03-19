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
                    Title = "Billing issue - invoice clarification",
                    ExpectedCategory = "billing",
                    ExpectedSeverity = "P3",
                    ExpectedOutcome = "Processed",
                    Description = "Caso amministrativo semplice, non bloccante.",
                    Payload = new CreateTicketInput
                    {
                        From = "maria.rossi@contoso-demo.com",
                        Subject = "Clarification needed on invoice total",
                        Body = """
                                Hello,
                                we would like clarification on the total amount shown on invoice INV-2026-0310.
                                We are not reporting a service outage and our operations are working normally.
                                Please confirm whether taxes and support fees were included correctly.
                                Customer: Contoso Demo
                                Invoice: INV-2026-0310
                                """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                },
                new()
                {
                    Key = "support-p2",
                    Title = "Support request - profile update help",
                    ExpectedCategory = "support",
                    ExpectedSeverity = "P2",
                    ExpectedOutcome = "Processed",
                    Description = "Richiesta di supporto chiara, senza blocco critico.",
                    Payload = new CreateTicketInput
                    {
                        From = "luca.bianchi@fabrikam-demo.com",
                        Subject = "Help needed updating company profile settings",
                        Body = """
                                Hi team,
                                I need help updating the company profile settings in the admin portal.
                                The platform is accessible and working correctly, but I am not sure where to change the notification preferences.
                                No urgent issue is involved.
                                User: luca.bianchi@fabrikam-demo.com
                                Department: Operations
                                """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                },
                new()
                {
                    Key = "technical-p3",
                    Title = "Technical issue - minor UI bug on dashboard",
                    ExpectedCategory = "technical",
                    ExpectedSeverity = "P3",
                    ExpectedOutcome = "Processed",
                    Description = "Bug tecnico lieve, non bloccante.",
                    Payload = new CreateTicketInput
                    {
                        From = "qa@northwind-demo.com",
                        Subject = "Dashboard filter label overlaps on mobile",
                        Body = """
                                Hello,
                                on mobile devices the dashboard filter label slightly overlaps with the search box.
                                Core features are working correctly and no users are blocked.
                                Please review this minor UI issue when possible.
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
                    Description = "Incidente critico, deve andare in review umana.",
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
                    Key = "support-p3",
                    Title = "Support request - export assistance",
                    ExpectedCategory = "support",
                    ExpectedSeverity = "P3",
                    ExpectedOutcome = "Processed",
                    Description = "Richiesta semplice e chiara.",
                    Payload = new CreateTicketInput
                    {
                        From = "helpdesk@adatum-demo.com",
                        Subject = "How to export monthly report",
                        Body = """
                                Hello,
                                we need guidance on how to export the monthly report in CSV format.
                                The application is working correctly and this is just a usage question.
                                Please share the correct steps.
                                """,
                        ReceivedAt = DateTime.UtcNow,
                        Source = "email"
                    }
                }
            };
        }
    }
}