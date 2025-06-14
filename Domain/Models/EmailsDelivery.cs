using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class EmailsDelivery
    {
        [Key]
        public long Id { get; set; }
        public required string RecipientEmail { get; set; }
        public required string RecipientUsername { get; set; }
        public required EmailTypeEnum EmailType { get; set; }
        public bool IsSent { get; set; } = false;
        public string? Message { get; set; }

        public int RetryCount { get; set; }

        public string Subject { get; set; }

        public string Link { get; set; }
        public string? EstimatedWait { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow.AddHours(1);
    }

    public enum EmailTypeEnum
    {
        Welcome,
        Reminder,
        Verify_Email,     
    }
}
