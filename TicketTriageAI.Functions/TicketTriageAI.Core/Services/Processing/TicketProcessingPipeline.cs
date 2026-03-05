using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Notifications;
using TicketTriageAI.Core.Services.Observability;
using TicketTriageAI.Core.Services.Text;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class TicketProcessingPipeline : ITicketProcessingPipeline
    {
        // Coordina il flusso di elaborazione del ticket: classificazione + persistenza
        // Traduce il ticket in ingresso in un documento pronto per Cosmos DB

        private readonly ITicketClassifier _classifier;
        private readonly ITicketRepository _repository;
        private readonly ITicketDocumentFactory _docFactory;
        private readonly ITicketStatusRepository _statusRepo;
        private readonly ILogger<TicketProcessingPipeline> _logger;
        private readonly ITicketNormalizationFactory _normalizationFactory;

        private readonly ITicketNotificationService _notifier;
        private readonly NotificationOptions _notificationOptions;

        private readonly double _confidenceThreshold;
        private readonly bool _forceReviewOnP1;

        private readonly ITicketTelemetry _telemetry;

        public TicketProcessingPipeline(
            ITicketClassifier classifier,
            ITicketRepository repository,
            ITicketDocumentFactory docFactory,
            ITicketStatusRepository statusRepo,
            ITicketNotificationService notifier,
            IOptions<NotificationOptions> notificationOptions,
            IOptions<TicketProcessingOptions> options,
            ILogger<TicketProcessingPipeline> logger,
            ITicketNormalizationFactory normalizationFactory,
            ITicketTelemetry telemtry)
        {
            _classifier = classifier;
            _repository = repository;
            _docFactory = docFactory;
            _statusRepo = statusRepo;
            _logger = logger;
            _normalizationFactory = normalizationFactory;
            _notifier = notifier;
            _notificationOptions = notificationOptions.Value;
            _telemetry = telemtry;

            var opt = options.Value;
            _confidenceThreshold = opt.ConfidenceThreshold;
            _forceReviewOnP1 = opt.ForceReviewOnP1;
        }

        public async Task ExecuteAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            try
            {
                await PatchProcessingAsync(ticket, ct);

                var (normalizedTicket, cleanBody) = Normalize(ticket);

                var result = await ClassifyAsync(normalizedTicket, ct);

                var (status, reason) = ApplyReviewPolicy(result);

                var meta = _classifier.GetMetadata(); // vedi step 3/4 sotto

                var doc = CreateDocument(ticket, result, meta, status, reason, cleanBody);

                await PersistAsync(doc, ct);

                if (status == TicketStatus.NeedsReview)
                {
                    await NotifyAsync(doc, ct);
                    await PatchNeedsReviewAsync(ticket, reason, ct);
                    _telemetry.TicketNeedsReview(doc);

                }
                else
                {
                    await PatchProcessedAsync(ticket, ct);
                    _telemetry.TicketNeedsReview(doc);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing failed. MessageId={MessageId}", ticket.MessageId);
                await _statusRepo.PatchFailedAsync(ticket.MessageId, TicketStatusReason.Exception, ct);

                _telemetry.TicketFailed(
                    correlationId: ticket.CorrelationId,
                    messageId: ticket.MessageId,
                    reason: TicketStatusReason.Exception,
                    ex: ex);

                throw;
            }
        }

        private Task PatchProcessingAsync(TicketIngested ticket, CancellationToken ct)
    => _statusRepo.PatchProcessingAsync(ticket.MessageId, ct);

        private (TicketIngested Normalized, string CleanBody) Normalize(TicketIngested ticket)
        {
            var (normalized, cleanBody) = _normalizationFactory.CreateNormalized(ticket);

            _logger.LogInformation(
                "Body normalized. OriginalLen={Orig} CleanLen={Clean}",
                ticket.Body?.Length ?? 0, cleanBody.Length);

            return (normalized, cleanBody);
        }

        private async Task<TicketTriageResult> ClassifyAsync(TicketIngested normalizedTicket, CancellationToken ct)
        {
            var result = await _classifier.ClassifyAsync(normalizedTicket, ct);

            _logger.LogInformation(
                "Triage result. Category={Category} Severity={Severity} Confidence={Confidence} NeedsHumanReview={NeedsHumanReview}",
                result.Category, result.Severity, result.Confidence, result.NeedsHumanReview);

            return result;
        }

        private (TicketStatus Status, string? Reason) ApplyReviewPolicy(TicketTriageResult result)
        {
            var lowConfidence = result.Confidence < _confidenceThreshold;
            var isP1 = string.Equals(result.Severity, "P1", StringComparison.OrdinalIgnoreCase);

            var needsReview =
                result.NeedsHumanReview
                || lowConfidence
                || (_forceReviewOnP1 && isP1);

            if (!needsReview)
                return (TicketStatus.Processed, null);

            if (lowConfidence) return (TicketStatus.NeedsReview, TicketStatusReason.LowConfidence);
            if (_forceReviewOnP1 && isP1) return (TicketStatus.NeedsReview, TicketStatusReason.SeverityCritical);
            return (TicketStatus.NeedsReview, TicketStatusReason.ModelFlagged);
        }

        private TicketDocument CreateDocument(
            TicketIngested originalTicket,
            TicketTriageResult result,
            ClassifierMetadata meta,
            TicketStatus status,
            string? reason,
            string cleanBody)
        {
            var doc = _docFactory.Create(originalTicket, result, meta);
            doc.Status = status;
            doc.StatusReason = reason;
            doc.CleanBody = cleanBody;
            return doc;
        }

        private Task PersistAsync(TicketDocument doc, CancellationToken ct)
            => _repository.UpsertAsync(doc, ct);

        private Task NotifyAsync(TicketDocument doc, CancellationToken ct)
            => _notifier.NotifyNeedsReviewAsync(doc, _notificationOptions.DashboardBaseUrl, ct);

        private Task PatchNeedsReviewAsync(TicketIngested ticket, string? reason, CancellationToken ct)
            => _statusRepo.PatchNeedsReviewAsync(ticket.MessageId, reason ?? TicketStatusReason.ModelFlagged, ct);

        private Task PatchProcessedAsync(TicketIngested ticket, CancellationToken ct)
            => _statusRepo.PatchProcessedAsync(ticket.MessageId, ct);
    }
}
