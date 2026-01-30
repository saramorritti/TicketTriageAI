using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public sealed class TicketDocument
    {
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("messageId")]
        [JsonProperty("messageId")]
        public string MessageId { get; init; } = default!;

        public string CorrelationId { get; init; } = default!;
        public string From { get; init; } = default!;
        public string Subject { get; init; } = default!;
        public string Body { get; init; } = default!;
        public DateTime ReceivedAt { get; init; }
        public string Source { get; init; } = "email";

        public string Category { get; init; } = default!;
        public string Severity { get; init; } = default!;
        public double Confidence { get; init; }
        public bool NeedsHumanReview { get; init; }
        public DateTime ProcessedAtUtc { get; init; } = DateTime.UtcNow;
        public string RawMessage { get; init; } = default!;   // il JSON originale ricevuto (audit)
        public string ClassifierName { get; init; } = "fake"; // es: "fake", "openai", "rules"
        public string ClassifierVersion { get; init; } = "1"; // versione 
        public string? Model { get; init; }                   // es: "gpt-4.1-mini"
        public TicketStatus Status { get; set; }
        public string? StatusReason { get; set; }
    }
}
