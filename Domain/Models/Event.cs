using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Event : BaseModel
    {
        public long CreatedByUserId { get; set; }

        public int AverageTimeToServe { get; set; }

        [ForeignKey("CreatedBy")]
        public SwiftLineUser SwiftLineUser { get; set; }
    }
}
