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
        // ========= COSMOS KEYS =========

        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        [JsonProperty("messageId")]
        [JsonPropertyName("messageId")]
        public string MessageId { get; init; } = default!;

        // ========= METADATA =========

        [JsonProperty("correlationId")]
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; init; } = default!;

        [JsonProperty("from")]
        [JsonPropertyName("from")]
        public string From { get; init; } = default!;

        [JsonProperty("subject")]
        [JsonPropertyName("subject")]
        public string Subject { get; init; } = default!;

        [JsonProperty("body")]
        [JsonPropertyName("body")]
        public string Body { get; init; } = default!;

        [JsonProperty("receivedAt")]
        [JsonPropertyName("receivedAt")]
        public DateTime ReceivedAt { get; init; }

        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; init; } = "email";

        // ========= TRIAGE =========

        [JsonProperty("category")]
        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonProperty("severity")]
        [JsonPropertyName("severity")]
        public string? Severity { get; init; }

        [JsonProperty("confidence")]
        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonProperty("needsHumanReview")]
        [JsonPropertyName("needsHumanReview")]
        public bool NeedsHumanReview { get; init; }

        [JsonProperty("summary")]
        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        [JsonProperty("entities")]
        [JsonPropertyName("entities")]
        public IReadOnlyList<string>? Entities { get; init; }


        // ========= CLASSIFIER AUDIT =========

        [JsonProperty("classifierName")]
        [JsonPropertyName("classifierName")]
        public string ClassifierName { get; init; } = "unknown";

        [JsonProperty("classifierVersion")]
        [JsonPropertyName("classifierVersion")]
        public string ClassifierVersion { get; init; } = "1";

        [JsonProperty("model")]
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        // ========= PROCESSING =========

        [JsonProperty("processedAtUtc")]
        [JsonPropertyName("processedAtUtc")]
        public DateTime ProcessedAtUtc { get; init; } = DateTime.UtcNow;

        [JsonProperty("rawMessage")]
        [JsonPropertyName("rawMessage")]
        public string RawMessage { get; init; } = default!;

        [JsonProperty("cleanBody")]
        [JsonPropertyName("cleanBody")]
        public string? CleanBody { get; set; }


        // ========= STATE MACHINE =========

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public TicketStatus Status { get; set; }

        [JsonProperty("statusReason")]
        [JsonPropertyName("statusReason")]
        public string? StatusReason { get; set; }
    }
}
