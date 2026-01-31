using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Common.Http
{
    public static class ApiMessages
    {
        public const string EmptyBody = "Request body is empty";
        public const string InvalidPayload = "Invalid request payload";
        public const string ValidationFailed = "Validation failed";
        public const string Accepted = "Ticket accepted for processing";
    }
}
