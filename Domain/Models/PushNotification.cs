using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    [Index(nameof(UserId), IsUnique = true)]
    public class PushNotification
    {
        [Key]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Subscrition { get; set; }

        public DateTime DateLastUpdated { get; set; }
        //public MyProperty { get; set; }
    }
}
