namespace TicketTriageAI.Dashboard.Options
{
    public sealed class IngestApiOptions
    {
        public string BaseUrl { get; init; } = default!;
        public string Route { get; init; } = "api/v1/tickets/ingest";
        public string? FunctionKey { get; init; }
        public int PollAttempts { get; init; } = 10;
        public int PollDelayMilliseconds { get; init; } = 1000;
    }
}
