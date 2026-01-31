using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Configuration
{
    public sealed class ServiceBusOptions
    {
        [Required]
        public string QueueName { get; init; } = default!;
    }
}
