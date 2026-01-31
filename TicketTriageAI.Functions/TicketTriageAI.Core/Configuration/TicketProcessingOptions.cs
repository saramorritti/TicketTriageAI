using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Configuration
{
    public sealed class TicketProcessingOptions
    {
        public double ConfidenceThreshold { get; init; } = 0.7;
        public bool ForceReviewOnP1 { get; init; } = true;
    }
}
