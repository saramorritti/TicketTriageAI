using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Services.Factories
{
    public sealed record ClassifierMetadata(string Name, string Version, string? Model);
}
