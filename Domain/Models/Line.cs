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
        public  long LineMemberId { get; set; }
        public  bool IsAttendedTo { get; set; }

        [ForeignKey("LineMemberId")]
        public LineMember LineMember { get; set; }
        public int TimeWaited { get; set; }

        public DateTime DateStartedBeingAttendedTo { get; set; }
        public DateTime DateCompletedBeingAttendedTo { get; set; }
        [AllowedValues(["pending","served","exitedByUser","exitedByAdmin"],ErrorMessage ="The value passed is not allowed")]
        public string Status { get; set; } = "pending";
    }
}
