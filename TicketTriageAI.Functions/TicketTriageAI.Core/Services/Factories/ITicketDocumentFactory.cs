using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Factories
{
    public interface ITicketDocumentFactory
    {
        TicketDocument Create(TicketIngested ticket, TicketTriageResult triage, ClassifierMetadata meta);
        TicketDocument CreateReceived(TicketIngested ticket);
        TicketDocument CreateFailedFromDlq(TicketIngested ticket, string reason);
    }
}
