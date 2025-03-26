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
        public string Title { get; set; }
        [MaxLength(1000)]
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public int AverageTime { get; set; }
        [NotMapped]
        public int AverageTimeToServeSeconds
        {
            get => AverageTime * 60;
        }
        [ForeignKey("CreatedBy")]
        public SwiftLineUser SwiftLineUser { get; set; }
        public required TimeOnly EventStartTime { get; set; }
        public required TimeOnly EventEndTime {get; set; }

        public int UsersInQueue { get; set; }
        [MaxLength(450)]
        [NotMapped]
        public string Organizer { get; set; }
        [NotMapped]
        public bool IsOngoing { get; set; }
    }
}
