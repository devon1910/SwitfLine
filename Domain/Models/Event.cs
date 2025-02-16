using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Event : BaseModel
    {
        [MaxLength(450)]
        public string Name { get; set; }
        public string CreatedBy { get; set; }

        public int AverageTimeToServe { get; set; }

        [ForeignKey("CreatedBy")]
        public SwiftLineUser SwiftLineUser { get; set; }

        public bool IsOngoing { get; set; } = true;
    }
}
