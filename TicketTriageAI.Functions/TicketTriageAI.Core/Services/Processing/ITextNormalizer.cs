using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketTriageAI.Core.Services.Processing
{
    public interface ITextNormalizer
    {
        string Normalize(string input);
    }
}
