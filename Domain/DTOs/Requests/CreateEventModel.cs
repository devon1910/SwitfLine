using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public record CreateEventModel(string Name, int AverageTimeToServe, string StartTime, string EndTime);

    public record EditEventReq(long EventId,string Name, int AverageTimeToServe, string StartTime, string EndTime);
}
