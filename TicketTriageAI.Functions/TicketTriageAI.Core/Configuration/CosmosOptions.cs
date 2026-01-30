using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Configuration
{
    public sealed class CosmosOptions
    {
        public string DatabaseName { get; init; } = default!;
        public string ContainerName { get; init; } = default!;
    }
}
