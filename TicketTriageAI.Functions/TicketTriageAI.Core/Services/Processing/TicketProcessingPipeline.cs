using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class TicketProcessingPipeline : ITicketProcessingPipeline
    {
        // Pipeline applicativa che gestisce il processing di un ticket già ingestato.
        // Orquestra la classificazione e applica le decisioni di business (es. human review).
        private readonly ITicketClassifier _classifier;
        private readonly ILogger<TicketProcessingPipeline> _logger;

        public TicketProcessingPipeline(
            ITicketClassifier classifier,
            ILogger<TicketProcessingPipeline> logger)
        {
            _classifier = classifier;
            _logger = logger;
        }

        public async Task ExecuteAsync(TicketIngested ticket, CancellationToken ct = default)
        {
            var result = await _classifier.ClassifyAsync(ticket, ct);

            _logger.LogInformation(
                "Triage result. Category={Category} Severity={Severity} Confidence={Confidence} NeedsHumanReview={NeedsHumanReview}",
                result.Category, result.Severity, result.Confidence, result.NeedsHumanReview);

            // MVP: qui dopo aggiungeremo persistenza/azioni (DB, notifiche, ecc.)
        }
    }
}
