using TicketTriageAI.Dashboard.Models;

namespace TicketTriageAI.Dashboard.Options
{
    public sealed class SampleTicketOption
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string ExpectedCategory { get; init; } = string.Empty;
        public string ExpectedSeverity { get; init; } = string.Empty;
        public string ExpectedOutcome { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;

        public CreateTicketInput Payload { get; init; } = new();
    }
}
