using System.ComponentModel.DataAnnotations;

namespace TicketTriageAI.Dashboard.Models
{
    public sealed class CreateTicketInput
    {
        [Required, EmailAddress]
        public string From { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string Source { get; set; } = "email";
    }
}
