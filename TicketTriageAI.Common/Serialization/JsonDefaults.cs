using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicketTriageAI.Common.Serialization
{
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
