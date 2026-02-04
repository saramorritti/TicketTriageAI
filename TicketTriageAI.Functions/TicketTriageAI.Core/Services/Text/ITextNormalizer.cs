using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Services.Text
{
    public interface ITextNormalizer
    {
        string Normalize(string input);
    }
}
