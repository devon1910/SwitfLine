using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class QueueMember : BaseModel
    {
        public BasePriority BasePriority { get; set; }

        public long UserId { get; set; }
        public long QueueId { get; set; }
        [ForeignKey("QueueId")]
        public Event Queue { get; set; }
    }

}
