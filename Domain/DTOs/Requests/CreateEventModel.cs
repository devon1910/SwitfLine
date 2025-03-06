using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public record CreateEventModel(string Title, string Description, int AverageTime, string StartTime, string EndTime);

    public record EditEventReq(long EventId,string Title, string Description, int AverageTime, string StartTime, string EndTime);
}
