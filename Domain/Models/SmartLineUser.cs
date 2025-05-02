using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class SwiftLineUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public bool IsInQueue { get; set; } = false;

        public long LastEventJoined { get; set; } = 0;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow.AddHours(1);
    }
}
