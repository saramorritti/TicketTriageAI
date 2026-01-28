using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Validators
{
    public sealed class TicketIngestedRequestValidator : AbstractValidator<TicketIngestedRequest>
    {
        public TicketIngestedRequestValidator()
        {
            RuleFor(x => x.MessageId).NotEmpty();
            RuleFor(x => x.From).NotEmpty();
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Body).NotEmpty();

            RuleFor(x => x.ReceivedAt)
                .Must(x => x != default)
                .WithMessage("receivedAt is required");
        }
    }
}
