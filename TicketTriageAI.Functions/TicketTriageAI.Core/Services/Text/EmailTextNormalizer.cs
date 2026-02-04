using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Services.Text
{
    public sealed class EmailTextNormalizer : ITextNormalizer
    {
        private static readonly Regex ReplyHeaderRegex = new(
            @"(?im)^\s*On\s.+wrote:\s*$",
            RegexOptions.Compiled);

        private static readonly Regex ForwardHeaderRegex = new(
            @"(?im)^\s*-{2,}\s*Forwarded message\s*-{2,}\s*$",
            RegexOptions.Compiled);

        private static readonly Regex SignatureSeparatorRegex = new(
            @"(?m)^\s*--\s*$",
            RegexOptions.Compiled);

        public string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input.Replace("\r\n", "\n").Trim();

            // 1) taglia tutto dopo "On ... wrote:"
            text = CutAtFirstMatch(text, ReplyHeaderRegex);

            // 2) taglia forwarded header se presente
            text = CutAtFirstMatch(text, ForwardHeaderRegex);

            // 3) taglia firma dopo "--"
            text = CutAtFirstMatch(text, SignatureSeparatorRegex);

            // 4) rimuovi righe quote ">"
            var lines = text.Split('\n')
                            .Where(l => !l.TrimStart().StartsWith(">"))
                            .ToArray();

            text = string.Join("\n", lines).Trim();

            // 5) collassa spazi vuoti multipli
            text = Regex.Replace(text, @"\n{3,}", "\n\n").Trim();

            return text;
        }

        private static string CutAtFirstMatch(string text, Regex regex)
        {
            var m = regex.Match(text);
            if (!m.Success) return text;

            return text[..m.Index].Trim();
        }
    }
}
