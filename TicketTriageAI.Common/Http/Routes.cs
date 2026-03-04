using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TicketTriageAI.Common.Http
{
    public static class Routes
    {
        public const string IngestTicketV1 = "v1/tickets/ingest";
    }
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
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
