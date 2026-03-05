using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Globalization;
using System.Text.Json;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;

namespace TicketTriageAI.Core.Services.Processing.AI
{
    public sealed class AzureOpenAITicketClassifier : ITicketClassifier
    {
        private readonly ChatClient _chat;
        private readonly AzureOpenAIClassifierOptions _opts;
        private readonly ILogger<AzureOpenAITicketClassifier> _logger;

        public AzureOpenAITicketClassifier(
            ChatClient chatClient,
            IOptions<AzureOpenAIClassifierOptions> opts,
            ILogger<AzureOpenAITicketClassifier> logger)
        {
            _chat = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<TicketTriageResult> ClassifyAsync(
            TicketIngested ticket,
            CancellationToken cancellationToken = default)
        {

            if (ticket is null) throw new ArgumentNullException(nameof(ticket));

            var systemPrompt = _opts.SystemPrompt;

            var userPrompt =
                $"Subject: {ticket.Subject}\n" +
                $"Body: {ticket.Body}\n" +
                $"From: {ticket.From}\n" +
                $"Source: {ticket.Source}\n";


            var options = new ChatCompletionOptions
            {
                Temperature = _opts.Temperature,
                MaxOutputTokenCount = _opts.MaxOutputTokenCount
            };


            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };
            
            ChatCompletion completion;
            try
            {
                completion = await _chat.CompleteChatAsync(messages, options, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Classifier call failed. Falling back.");
                return Fallback();
            }

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
            catch (JsonException ex)
            {
                var safe = content.Length > 600 ? content.Substring(0, 600) : content;
                _logger.LogWarning(ex, "Invalid JSON from classifier. Content(first600)={Content}", safe);
                return Fallback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Classifier parsing failed. Falling back.");
                return Fallback();
            }
        }

        public ClassifierMetadata GetMetadata() =>
        new(Name: nameof(AzureOpenAITicketClassifier),
            Version: _opts.ClassifierVersion,
            Model: _opts.DeploymentName);
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

