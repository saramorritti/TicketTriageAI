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
using TicketTriageAI.Core.Services.Text;

namespace TicketTriageAI.Core.Services.Processing.AI
{
    public sealed class AzureOpenAITicketClassifier : ITicketClassifier
    {
        private readonly ChatClient _chat;
        private readonly double _confidenceThreshold;
        private readonly ITextNormalizer _textNormalizer;

        public AzureOpenAITicketClassifier(ChatClient chatClient, IConfiguration configuration, ITextNormalizer textNormalizer)
        {
            _chat = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _textNormalizer = textNormalizer;

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
                "needsHumanReview (true|false), " +
                "summary (string, max 200 chars), " +
                "entities (array of strings). " +
                "No markdown. No explanations. No extra text.";

            var cleanBody = _textNormalizer.Normalize(ticket.Body);

            var userPrompt =
                $"Subject: {ticket.Subject}\n" +
                $"Body: {cleanBody}\n" +
                $"From: {ticket.From}\n" +
                $"Source: {ticket.Source}\n";


            var options = new ChatCompletionOptions
            {
                Temperature = 0.0f,
                MaxOutputTokenCount = 350
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

                var summary = root.TryGetProperty("summary", out var sEl) ? sEl.GetString() : string.Empty;

                var entities = Array.Empty<string>();
                if (root.TryGetProperty("entities", out var eEl) && eEl.ValueKind == JsonValueKind.Array)
                {
                    entities = eEl.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()!)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                }

                return new TicketTriageResult
                {
                    Category = NormalizeCategory(category),
                    Severity = NormalizeSeverity(severity),
                    Confidence = confidence,
                    NeedsHumanReview = needsHumanReview,
                    Summary = (summary ?? string.Empty).Trim(),
                    Entities = entities
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
            NeedsHumanReview = true,
            Summary = string.Empty,
            Entities = Array.Empty<string>()
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

