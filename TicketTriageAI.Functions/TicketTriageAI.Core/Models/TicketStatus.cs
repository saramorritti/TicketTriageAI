using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public enum TicketStatus
    {
        Received = 0,
        Processed = 1,
        NeedsReview = 2,
        Failed = 3
    }
}
