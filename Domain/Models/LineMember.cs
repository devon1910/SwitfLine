using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class LineMember : BaseModel
    {
        public BasePriority BasePriority { get; set; }

        public string UserId { get; set; }
        public long EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; }
        [ForeignKey("UserId")]
        public SwiftLineUser SwiftLineUser { get; set; }
    }

}
