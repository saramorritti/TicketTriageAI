using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Configuration
{
    public sealed class CosmosOptions
    {
        [Required]
        public string DatabaseName { get; init; } = default!;

        [Required]
        public string ContainerName { get; init; } = default!;
    }
}
