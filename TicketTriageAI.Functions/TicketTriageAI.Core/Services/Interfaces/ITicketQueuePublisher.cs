using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;

namespace TicketTriageAI.Core.Services.Interfaces
{
    public interface ITicketQueuePublisher
    {
        Task PublishAsync(TicketIngested ticket, CancellationToken ct = default);
    }
}
