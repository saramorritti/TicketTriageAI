using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Notifications
{
    public sealed class LoggingTicketNotificationService : ITicketNotificationService
    {
        private readonly ILogger<LoggingTicketNotificationService> _logger;

        public LoggingTicketNotificationService(ILogger<LoggingTicketNotificationService> logger)
            => _logger = logger;

        public Task NotifyNeedsReviewAsync(TicketDocument ticket, string dashboardUrl, CancellationToken ct = default)
        {
            var link = $"{dashboardUrl.TrimEnd('/')}/Tickets/Detail?messageId={ticket.MessageId}";

            _logger.LogWarning(
                "NOTIFY NeedsReview. MessageId={MessageId} Severity={Severity} Confidence={Confidence} Link={Link} Reason={Reason}",
                ticket.MessageId, ticket.Severity, ticket.Confidence, link, ticket.StatusReason);

            return Task.CompletedTask;
        }
    }
}
