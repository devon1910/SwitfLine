using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
  
    public class QueueEntry
    {
        public long EventId { get; set; }
        public string  DateStartedBeingAttendedTo { get; set; }
        public string DateCompletedBeingAttendedTo { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; }
        public int  AvgServiceTimeWhenJoined { get; set; }
        public string DayOfWeek { get; set; }
        public int NumActiveServersWhenJoined { get; set; }
        public int PositionInQueueWhenJoined { get; set; }
        public string TimeOfDay { get; set; }
        public int TotalPeopleInQueueWhenJoined { get; set; }
        public string UserId { get; set; }
    }


    public class WaitTimePrediction
    {
        public float Score { get; set; }  // minutes  
    }
}
