using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Notifications
{
    public interface ITicketNotificationService
    {
        Task NotifyNeedsReviewAsync(TicketDocument ticket, string dashboardUrl, CancellationToken ct = default);
    }
}
