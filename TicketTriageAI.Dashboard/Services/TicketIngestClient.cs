using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using TicketTriageAI.Dashboard.Models;
using TicketTriageAI.Dashboard.Options;

namespace TicketTriageAI.Dashboard.Services
{
    public sealed class TicketIngestClient : ITicketIngestClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly IngestApiOptions _options;
        private readonly ILogger<TicketIngestClient> _logger;

        public TicketIngestClient(
            HttpClient httpClient,
            IOptions<IngestApiOptions> options,
            ILogger<TicketIngestClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IngestCallResult> CreateAsync(
            CreateTicketInput input,
            string messageId,
            CancellationToken ct = default)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            if (string.IsNullOrWhiteSpace(messageId))
                throw new ArgumentException("MessageId non valido.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new InvalidOperationException("IngestApi:BaseUrl non configurato.");

            if (string.IsNullOrWhiteSpace(_options.Route))
                throw new InvalidOperationException("IngestApi:Route non configurato.");

            var baseUrl = _options.BaseUrl.Trim();
            var route = _options.Route.Trim().TrimStart('/');

            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
                baseUrl += "/";

            var url = new Uri(new Uri(baseUrl, UriKind.Absolute), route);

            var payload = new
            {
                from = input.From,
                subject = input.Subject,
                body = input.Body,
                receivedAt = input.ReceivedAt,
                source = input.Source
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Idempotency-Key", messageId);

            if (!string.IsNullOrWhiteSpace(_options.FunctionKey))
            {
                request.Headers.Add("x-functions-key", _options.FunctionKey.Trim());
            }

            _logger.LogInformation(
                "Calling ingest function at {Url}. MessageId: {MessageId}, Subject: {Subject}",
                url,
                messageId,
                input.Subject);

            try
            {
                using var response = await _httpClient.SendAsync(request, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation(
                    "Ingest function responded with status {StatusCode}. MessageId: {MessageId}",
                    (int)response.StatusCode,
                    messageId);

                return new IngestCallResult
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = responseBody
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Ingest request cancelled by caller. MessageId: {MessageId}",
                    messageId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while calling ingest function at {Url}. MessageId: {MessageId}",
                    url,
                    messageId);

                return new IngestCallResult
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    ResponseBody = $"Errore durante la chiamata alla ingest function: {ex.Message}"
                };
            }
        }
    }
}