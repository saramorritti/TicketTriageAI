using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using TicketTriageAI.Dashboard.Models;
using TicketTriageAI.Dashboard.Options;

namespace TicketTriageAI.Dashboard.Services
{
    public sealed class TicketIngestClient : ITicketIngestClient
    {
        private readonly HttpClient _httpClient;
        private readonly IngestApiOptions _options;

        public TicketIngestClient(HttpClient httpClient, IOptions<IngestApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<IngestCallResult> CreateAsync(CreateTicketInput input, string messageId, CancellationToken ct = default)
        {
            var url = new Uri(new Uri(_options.BaseUrl), _options.Route);

            var payload = new
            {
                from = input.From,
                subject = input.Subject,
                body = input.Body,
                receivedAt = input.ReceivedAt,
                source = input.Source
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Headers.Add("Idempotency-Key", messageId);

            if (!string.IsNullOrWhiteSpace(_options.FunctionKey))
                request.Headers.Add("x-functions-key", _options.FunctionKey);

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            return new IngestCallResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody
            };
        }
    }
}
