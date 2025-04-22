using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public record TurnstileResponse(bool success, string challenge_ts, string hostname, List<string> error_codes);
}
