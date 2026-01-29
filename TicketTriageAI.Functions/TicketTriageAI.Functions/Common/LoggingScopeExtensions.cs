using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Functions.Common
{
    public static class LoggingScopeExtensions
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            private NullScope() { }
            public void Dispose() { }
        }

        public static IDisposable BeginCorrelationScope(
            this ILogger logger,
            string correlationId)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }) ?? NullScope.Instance;
        }

        public static IDisposable BeginCorrelationScope(
            this ILogger logger,
            string correlationId,
            string messageId)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["MessageId"] = messageId
            }) ?? NullScope.Instance;
        }
    }
}
