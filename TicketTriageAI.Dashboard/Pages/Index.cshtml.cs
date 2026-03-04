using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TicketTriageAI.Dashboard.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
            => RedirectToPage("/Tickets/Index");
    }
}
