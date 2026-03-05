using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Notifications
{
    public sealed class ServiceBusTicketNotificationService : ITicketNotificationService
    {
        private readonly ServiceBusSender _sender;
        private readonly NotificationOptions _opts;
        private readonly ILogger<ServiceBusTicketNotificationService> _logger;

        public ServiceBusTicketNotificationService(
            [FromKeyedServices("notify")] ServiceBusSender sender,
            IOptions<NotificationOptions> opts,
            ILogger<ServiceBusTicketNotificationService> logger)
        {
            _sender = sender;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task NotifyNeedsReviewAsync(TicketDocument ticket, string dashboardUrl, CancellationToken ct = default)
        {

            var baseUrl = string.IsNullOrWhiteSpace(dashboardUrl) ? _opts.DashboardBaseUrl : dashboardUrl;
            var link = $"{baseUrl.TrimEnd('/')}/Tickets/Detail?messageId={ticket.MessageId}";

            var payload = new
            {
                messageId = ticket.MessageId,
                correlationId = ticket.CorrelationId,
                from = ticket.From,
                subject = ticket.Subject,
                category = ticket.Category,
                severity = ticket.Severity,
                confidence = ticket.Confidence,
                statusReason = ticket.StatusReason,
                dashboardLink = link,
                createdAtUtc = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);

            var msg = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                Subject = "ticket.needsReview",
                MessageId = ticket.MessageId,
                CorrelationId = ticket.CorrelationId
            };

            await _sender.SendMessageAsync(msg, ct);

            _logger.LogInformation("Enqueued Teams notification. MessageId={MessageId} Queue={Queue}", ticket.MessageId, _opts.NotifyQueueName);
        }
    }
}
