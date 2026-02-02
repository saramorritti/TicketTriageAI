using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Services.Processing
{
    public sealed class DefaultTextNormalizer : ITextNormalizer
    {
        public string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input;

            // rimuove firme comuni
            text = Regex.Split(text, @"\n--\s*\n|\nGrazie[,]?.*", RegexOptions.IgnoreCase)[0];

            // rimuove quote email
            text = Regex.Split(text, @"\nOn .* wrote:|\nIl .* ha scritto:", RegexOptions.IgnoreCase)[0];

            // compatta spazi
            text = Regex.Replace(text, @"\s{2,}", " ");

            return text.Trim();
        }
    }
}
