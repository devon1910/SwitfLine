using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public record SignupModel
    {

        [Required]
        [MaxLength(50)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Password { get; set; } = string.Empty;
    }
}
