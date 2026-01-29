using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Processing;

namespace TicketTriageAI.Core.Services.Processing.AI
{
    public sealed class AzureOpenAITicketClassifier : ITicketClassifier
    {
        private readonly ChatClient _chat;
        private readonly double _confidenceThreshold;

        public AzureOpenAITicketClassifier(ChatClient chatClient, IConfiguration configuration)
        {
            _chat = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

            var thresholdRaw = configuration["AzureOpenAIConfidenceThreshold"] ?? "0.7";
            _confidenceThreshold = double.TryParse(
                thresholdRaw,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : 0.7;
        }

        public async Task<TicketTriageResult> ClassifyAsync(
            TicketIngested ticket,
            CancellationToken cancellationToken = default)
        {
            if (ticket is null) throw new ArgumentNullException(nameof(ticket));

            var systemPrompt =
                "You are a strict ticket triage classifier. " +
                "Return ONLY a valid JSON object with EXACT keys: " +
                "category (billing|support|technical|other), " +
                "severity (P1|P2|P3), " +
                "confidence (0..1), " +
                "needsHumanReview (true|false). " +
                "No markdown. No explanations. No extra text.";

            var userPrompt =
                $"Subject: {ticket.Subject}\n" +
                $"Body: {ticket.Body}\n" +
                $"From: {ticket.From}\n" +
                $"Source: {ticket.Source}\n";

            var options = new ChatCompletionOptions
            {
                Temperature = 0.0f,
                MaxOutputTokenCount = 200
            };

            ChatCompletion completion = await _chat.CompleteChatAsync(
                new ChatMessage[]
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                },
                options,
                cancellationToken);

            // Recupero testo (defensivo, perché l’SDK può esporre content in più modi a seconda versione)
            var content =
                completion?.Content?.FirstOrDefault()?.Text?.Trim()
                ?? completion?.Content?.FirstOrDefault()?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(content))
                return Fallback();

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                var category = root.TryGetProperty("category", out var catEl) ? catEl.GetString() : "other";
                var severity = root.TryGetProperty("severity", out var sevEl) ? sevEl.GetString() : "P3";
                var confidence = root.TryGetProperty("confidence", out var cEl) ? cEl.GetDouble() : 0.0;
                var needsHumanReview = root.TryGetProperty("needsHumanReview", out var nEl) && nEl.GetBoolean();

                if (confidence < _confidenceThreshold)
                    needsHumanReview = true;

                return new TicketTriageResult
                {
                    Category = NormalizeCategory(category),
                    Severity = NormalizeSeverity(severity),
                    Confidence = confidence,
                    NeedsHumanReview = needsHumanReview
                };
            }
            catch
            {
                return Fallback();
            }
        }

        private static TicketTriageResult Fallback() => new TicketTriageResult
        {
            Category = "other",
            Severity = "P3",
            Confidence = 0.0,
            NeedsHumanReview = true
        };

        private static string NormalizeCategory(string? value)
        {
            var v = (value ?? "other").Trim().ToLowerInvariant();
            return v is "billing" or "support" or "technical" ? v : "other";
        }

        private static string NormalizeSeverity(string? value)
        {
            var v = (value ?? "P3").Trim().ToUpperInvariant();
            return v is "P1" or "P2" or "P3" ? v : "P3";
        }
    }
}

