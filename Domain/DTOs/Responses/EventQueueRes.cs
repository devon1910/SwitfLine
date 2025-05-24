using Domain.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public record EventQueueRes(List<Line> linesMembersInQueue, List<Line> pastLineMembers, bool IsEventPaused, int pageCountInQueue,int pageCountPastMembers, int TotalServed, int AverageWaitTime, int dropOffRate, object attendanceData);
}
