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
        private readonly ITextNormalizer _normalizer;


        private readonly double _confidenceThreshold;
        private readonly bool _forceReviewOnP1;

        public TicketProcessingPipeline(
            ITicketClassifier classifier,
            ITicketRepository repository,
            ITicketDocumentFactory docFactory,
            ITicketStatusRepository statusRepo,
            IOptions<TicketProcessingOptions> options,
            ILogger<TicketProcessingPipeline> logger,
            ITextNormalizer normalizer)
        {
            _classifier = classifier;
            _repository = repository;
            _docFactory = docFactory;
            _statusRepo = statusRepo;
            _logger = logger;
            _normalizer = normalizer;

            var opt = options.Value;
            _confidenceThreshold = opt.ConfidenceThreshold;
            _forceReviewOnP1 = opt.ForceReviewOnP1;
        }

        public async Task ExecuteAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            try
            {
                await _statusRepo.PatchProcessingAsync(ticket.MessageId, ct);

                var cleanedBody = _normalizer.Normalize(ticket.Body);

                var normalizedTicket = new TicketIngested
                {
                    MessageId = ticket.MessageId,
                    CorrelationId = ticket.CorrelationId,
                    From = ticket.From,
                    Subject = ticket.Subject,
                    Body = cleanedBody,
                    ReceivedAt = ticket.ReceivedAt,
                    Source = ticket.Source,
                    RawMessage = ticket.RawMessage
                };

                var result = await _classifier.ClassifyAsync(normalizedTicket, ct);
                _logger.LogInformation(
                    "Body normalized. OriginalLen={Orig} CleanLen={Clean}", 
                    ticket.Body?.Length ?? 0, cleanedBody.Length);


                _logger.LogInformation(
                    "Triage result. Category={Category} Severity={Severity} Confidence={Confidence} NeedsHumanReview={NeedsHumanReview}",
                    result.Category, result.Severity, result.Confidence, result.NeedsHumanReview);

                var meta = new ClassifierMetadata(
                    Name: _classifier.GetType().Name,
                    Version: "1",
                    Model: null);

                // ---- Review policy (qui risolvi l’incoerenza) ----
                var lowConfidence = result.Confidence < _confidenceThreshold;
                var isP1 = string.Equals(result.Severity, "P1", StringComparison.OrdinalIgnoreCase);

                var needsReview =
                    result.NeedsHumanReview
                    || lowConfidence
                    || (_forceReviewOnP1 && isP1);

                var status = needsReview ? TicketStatus.NeedsReview : TicketStatus.Processed;

                string? reason = null;
                if (needsReview)
                {
                    if (lowConfidence) reason = TicketStatusReason.LowConfidence;
                    else if (_forceReviewOnP1 && isP1) reason = TicketStatusReason.SeverityCritical; 
                    else reason = TicketStatusReason.ModelFlagged;
                }

                var doc = _docFactory.Create(ticket, result, meta);
                doc.Status = status;
                doc.StatusReason = reason;
                doc.CleanBody = cleanedBody;

                await _repository.UpsertAsync(doc, ct);

                if (status == TicketStatus.NeedsReview)
                {
                    // qui niente ! : se reason fosse null, metti un fallback sicuro
                    await _statusRepo.PatchNeedsReviewAsync(ticket.MessageId, reason ?? TicketStatusReason.ModelFlagged, ct);
                }
                else
                {
                    await _statusRepo.PatchProcessedAsync(ticket.MessageId, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing failed. MessageId={MessageId}", ticket.MessageId);

                await _statusRepo.PatchFailedAsync(ticket.MessageId, TicketStatusReason.Exception, ct);
                throw;
            }
        }
    }
}
