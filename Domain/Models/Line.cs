using System;
using System.Collections.Generic;
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
        public DateTime DateAttendedTo { get; set; }
    }
}
