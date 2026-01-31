using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Models
{
    public static class TicketStatusReason
    {
        public const string DeadLetter = "DeadLetter";
        public const string LowConfidence = "LowConfidence";
        public const string ModelFlagged = "ModelFlagged";
        public const string Exception = "Exception";
        public const string SeverityCritical = "SeverityCritical";

    }
}
