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
using TicketTriageAI.Common.Serialization;
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

            var notification = new TicketNotificationMessage
            {
                MessageId = ticket.MessageId,
                CorrelationId = ticket.CorrelationId,
                From = ticket.From,
                Subject = ticket.Subject,
                Category = ticket.Category,
                Severity = ticket.Severity,
                Confidence = ticket.Confidence,
                StatusReason = ticket.StatusReason,
                DashboardLink = link,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(notification, JsonDefaults.Options);

            var msg = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                Subject = "ticket.needsReview",
                MessageId = notification.MessageId,
                CorrelationId = notification.CorrelationId
            };

            await _sender.SendMessageAsync(msg, ct);

            _logger.LogInformation("Enqueued Teams notification. MessageId={MessageId} Queue={Queue}", ticket.MessageId, _opts.NotifyQueueName);
        }
    }
}
