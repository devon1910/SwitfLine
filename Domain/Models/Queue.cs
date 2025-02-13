using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Queue: BaseModel
    {
        public long EventId { get; set; }
        public  long QueueMemberId { get; set; }
        public  bool IsAttendedTo { get; set; }

        [ForeignKey("EventId")]
        public Event Event { get; set; }
        [ForeignKey("QueueMemberId")]
        public QueueMember QueueMember { get; set; }
    }
}
