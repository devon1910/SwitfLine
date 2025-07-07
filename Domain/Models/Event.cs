using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        [Range(1, 60, ErrorMessage ="Average time to serve must be between 1-60")]
        public int AverageTime { get; set; }
        public int AverageTimeToServeSeconds
        {
            get; set;
        }
        [ForeignKey("CreatedBy")]
        public SwiftLineUser SwiftLineUser { get; set; }
        public required TimeOnly EventStartTime { get; set; }
        public required TimeOnly EventEndTime {get; set; }

        public int Capacity { get; set; }
        [Range(1,20,ErrorMessage ="Staff Count must be between 1-20")]
        public int StaffCount { get; set; }

        public int UsersInQueue { get; set; }
        [MaxLength(450)]
        [NotMapped]
        public string Organizer { get; set; }
        [NotMapped]
        public bool HasStarted { get; set; }

        public bool IsDeleted { get; set; } = false;
        [DefaultValue(false)]
        public bool AllowAnonymousJoining { get; set; }

        public bool AllowAutomaticSkips { get; set; } = true;
    }
}
