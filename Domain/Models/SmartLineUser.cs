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

        public bool isInQueue { get; set; } = false;    
    }
}
