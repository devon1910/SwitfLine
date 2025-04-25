using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Feedback
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public List<string> Tags { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
