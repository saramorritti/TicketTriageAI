using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Ingest
{
    public interface ITicketIngestService
    {
        Task<(TicketIngestedRequest? Request, ValidationResult Validation)>
            ParseAndValidateAsync(string body);
    }
}
