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
            "No markdown. No explanations. No extra text.";
    }
}
