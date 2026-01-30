using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class TicketProcessingPipeline : ITicketProcessingPipeline
    {
        // Coordina il flusso di elaborazione del ticket: classificazione + persistenza
        // Traduce il ticket in ingresso in un documento pronto per Cosmos DB

        private readonly ITicketClassifier _classifier;
        private readonly ITicketRepository _repository;
        private readonly ITicketDocumentFactory _docFactory;
        private readonly ILogger<TicketProcessingPipeline> _logger;

        public TicketProcessingPipeline(
            ITicketClassifier classifier,
            ITicketRepository repository,
            ITicketDocumentFactory docFactory,
            ILogger<TicketProcessingPipeline> logger)
        {
            _classifier = classifier;
            _repository = repository;
            _docFactory = docFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            var result = await _classifier.ClassifyAsync(ticket, ct);

            _logger.LogInformation(
                "Triage result. Category={Category} Severity={Severity} Confidence={Confidence} NeedsHumanReview={NeedsHumanReview}",
                result.Category, result.Severity, result.Confidence, result.NeedsHumanReview);

            // metadata auditabile (per ora hardcoded, poi lo renderemo parte del classifier)
            var meta = new ClassifierMetadata(
                Name: _classifier.GetType().Name,
                Version: "1",
                Model: null
            );

            var doc = _docFactory.Create(ticket, result, meta);

            // STATUS (nuovo)
            doc.Status = result.NeedsHumanReview
                ? TicketStatus.NeedsReview
                : TicketStatus.Processed;

            doc.StatusReason = result.NeedsHumanReview
                ? (result.Confidence < 0.7 ? "LowConfidence" : "ModelFlagged")
                : null;

            await _repository.UpsertAsync(doc, ct);

        }
    }
}
