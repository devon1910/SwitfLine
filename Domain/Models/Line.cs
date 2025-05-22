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
        public int PositionInQueueWhenJoined { get; set; }
        public int AvgServiceTimeWhenJoined { get; set; }
        public int NumActiveServersWhenJoined { get; set; }
        public int TotalPeopleInQueueWhenJoined { get; set; }
        public double TimeWaited { get; set; }
        public string TimeOfDay { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
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
}
