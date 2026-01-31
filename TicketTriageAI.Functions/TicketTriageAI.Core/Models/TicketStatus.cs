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
        Processing = 1,
        Processed = 2,
        NeedsReview = 3,
        Failed = 4
    }
}
