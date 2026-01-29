using FluentValidation;
using FluentValidation.Results;
using System.Text.Json;
using TicketTriageAI.Core.Models;
using static System.Net.WebRequestMethods;

namespace TicketTriageAI.Core.Services.Ingest
{
    public sealed class TicketIngestService : ITicketIngestService
    {
        // Servizio applicativo di parsing e validazione dell’input grezzo (JSON) in TicketIngestedRequest.
        // Mantiene il trigger HTTP “thin”: qui si concentra l’interpretazione del payload + validation.
        
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
