using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Configuration
{
    public sealed class AzureOpenAIClassifierOptions
    {
        public float Temperature { get; init; } = 0.0f;
        public int MaxOutputTokenCount { get; init; } = 350;

        public string SystemPrompt { get; init; } =
        "You are a strict ticket triage classifier. " +
        "Return ONLY a valid JSON object with EXACT keys: " +
        "category (billing|support|technical|other), " +
        "severity (P1|P2|P3), " +
        "confidence (0..1), " +
        "needsHumanReview (true|false), " +
        "summary (string, max 200 chars), " +
        "entities (array of strings). " +
        "Set needsHumanReview to true ONLY when the ticket is ambiguous, suspicious, security-related, incomplete, or requires manual judgment. " +
        "Set needsHumanReview to false for clear and actionable billing, support, or technical tickets. " +
        "Use severity P1 only for critical production incidents, widespread outages, or revenue-blocking failures. " +
        "Use P2 for important but limited-impact issues. " +
        "Use P3 for minor issues, informational requests, UI bugs, or standard support requests. " +
        "No markdown. No explanations. No extra text.";

        public string DeploymentName { get; init; } = "ticket-triage";
        public string ClassifierVersion { get; init; } = "1";
    }
}
