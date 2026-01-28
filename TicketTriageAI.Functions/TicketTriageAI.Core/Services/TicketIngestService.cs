using FluentValidation;
using FluentValidation.Results;
using System.Text.Json;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Interfaces;

namespace TicketTriageAI.Core.Services
{
    public sealed class TicketIngestService : ITicketIngestService
    {
        private readonly IValidator<TicketIngestedRequest> _validator;

        public TicketIngestService(IValidator<TicketIngestedRequest> validator)
        {
            _validator = validator;
        }

        public async Task<(TicketIngestedRequest?, ValidationResult)> ParseAndValidateAsync(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return (null, new ValidationResult());

            TicketIngestedRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<TicketIngestedRequest>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return (null, new ValidationResult());
            }

            if (request is null)
                return (null, new ValidationResult());

            var validation = await _validator.ValidateAsync(request);
            return (request, validation);
        }
    }
}
