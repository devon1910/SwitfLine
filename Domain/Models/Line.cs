using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Line: BaseModel
    {
        public  bool IsAttendedTo { get; set; }
       
        public string UserId { get; set; }
        public long EventId { get; set; }
        public int AvgServiceTimeWhenJoined { get; set; }
        public int NumActiveServersWhenJoined { get; set; }
        public double TimeWaited { get; set; }
        public TimeOfDayEnum TimeOfDay { get; set; }
        public DayOfWeekEnum DayOfWeek { get; set; } 
        public DateTime DateStartedBeingAttendedTo { get; set; }
        public DateTime DateCompletedBeingAttendedTo { get; set; }

        public int EffectiveQueuePosition { get; set; }

        [AllowedValues(["pending", "served", "exitedByUser", "exitedByAdmin"], ErrorMessage = "The value passed is not allowed")]
        public string Status { get; set; } = "pending";

        [ForeignKey("EventId")]
        public Event Event { get; set; }

        [ForeignKey("UserId")]
        public SwiftLineUser SwiftLineUser { get; set; }

       
    }
    public enum DayOfWeekEnum
    {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    public enum TimeOfDayEnum
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }
}
