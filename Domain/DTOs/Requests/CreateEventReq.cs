using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public record CreateEventReq(string Name, int AverageTimeToServe, string StartTime, string EndTime);

    public record EditEventReq(long EventId,string Name, int AverageTimeToServe, string StartTime, string EndTime);
}
