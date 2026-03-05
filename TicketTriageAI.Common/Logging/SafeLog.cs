using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Common.Logging
{
    public static class SafeLog
    {
        public static string Truncate(string? s, int max = 256)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        public static string Sha256Hex(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // Output utile per debug senza esporre PII
        public static string SafePayload(string? raw, int preview = 256)
        {
            if (string.IsNullOrEmpty(raw)) return "raw=<empty>";
            return $"len={raw.Length} sha256={Sha256Hex(raw)} preview={Truncate(raw, preview)}";
        }
    }
}
