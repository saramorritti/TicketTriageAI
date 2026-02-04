using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Dashboard.Repositories;

namespace TicketTriageAI.Dashboard.Pages.Tickets
{
    public sealed class DetailModel : PageModel
    {
        private readonly ITicketReadRepository _repo;

        public DetailModel(ITicketReadRepository repo) => _repo = repo;

        public TicketDocument? Ticket { get; private set; }

        public async Task<IActionResult> OnGetAsync(string messageId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return BadRequest();

            Ticket = await _repo.GetAsync(messageId, ct);

            if (Ticket is null)
                return NotFound();

            return Page();
        }
    }
}

