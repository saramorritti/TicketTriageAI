using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Factories
{
    public interface ITicketIngestedFactory
    {
        TicketIngested Create(TicketIngestedRequest request, string correlationId, string messageId, string? rawMessage = null);
    }
}
