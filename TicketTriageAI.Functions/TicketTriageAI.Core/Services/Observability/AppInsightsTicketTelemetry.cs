using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using TicketTriageAI.Core.Models;


namespace TicketTriageAI.Core.Services.Observability
{
    public sealed class AppInsightsTicketTelemetry : ITicketTelemetry
    {
        private readonly TelemetryClient _tc;

        public AppInsightsTicketTelemetry(TelemetryClient tc) => _tc = tc;

        public void TicketIngested(TicketIngested ticket)
        {
            _tc.TrackEvent("ticket_ingested", new Dictionary<string, string>
            {
                ["correlationId"] = ticket.CorrelationId,
                ["messageId"] = ticket.MessageId,
                ["source"] = ticket.Source ?? ""
            });
        }

        public void TicketProcessed(TicketDocument doc)
        {
            _tc.TrackEvent("ticket_processed", Props(doc, reason: doc.StatusReason), Metrics(doc));
        }

        public void TicketNeedsReview(TicketDocument doc)
        {
            _tc.TrackEvent("ticket_needs_review", Props(doc, reason: doc.StatusReason), Metrics(doc));
        }

        public void TicketFailed(string correlationId, string messageId, string reason, Exception? ex = null)
        {
            _tc.TrackEvent("ticket_failed", new Dictionary<string, string>
            {
                ["correlationId"] = correlationId,
                ["messageId"] = messageId,
                ["reason"] = reason
            });

            if (ex != null)
                _tc.TrackException(ex, new Dictionary<string, string>
                {
                    ["correlationId"] = correlationId,
                    ["messageId"] = messageId,
                    ["reason"] = reason
                });
        }

        private static Dictionary<string, string> Props(TicketDocument doc, string? reason)
            => new()
            {
                ["correlationId"] = doc.CorrelationId,
                ["messageId"] = doc.MessageId,
                ["category"] = doc.Category ?? "",
                ["severity"] = doc.Severity ?? "",
                ["reason"] = reason ?? ""
            };

        private static Dictionary<string, double> Metrics(TicketDocument doc)
            => new()
            {
                ["confidence"] = doc.Confidence
            };
    }
}
