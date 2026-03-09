namespace TicketTriageAI.Dashboard.Models
{
    public sealed class IngestCallResult
    {
        public bool IsSuccess { get; init; }
        public int StatusCode { get; init; }
        public string? ResponseBody { get; init; }
    }
}
