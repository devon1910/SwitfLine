using Domain.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public record EventQueueRes(
        List<Line> linesMembersInQueue, List<Line> pastLineMembers, bool IsEventPaused, int pageCountInQueue, int pageCountPastMembers,
        int TotalServed, int AverageWaitTime, int dropOffRate, object attendanceData, object dropOffRateTrend, object dropOffReasons,
        object peakArrivalPeriodData
        );

    public class AttendanceData
    {
        public int Month { get; set; }

        public int ServedCount { get; set; }

        public int AttendeesCount { get; set; }
    }
    public class DropOffRateTrend
    {
        public int Month { get; set; }
        public double DropOffRate { get; set; }
    }

    public class DropOffReason
    {
        public string Reason { get; set; }
        public int Count { get; set; }
    }
    public class PeakArrivalPeriodData
    {
        public TimeOfDayEnum TimeOfDay { get; set; } // e.g., "Morning", "Afternoon", "Evening", "Night"
        public int Count { get; set; } // Number of attendees during that period
    }

}