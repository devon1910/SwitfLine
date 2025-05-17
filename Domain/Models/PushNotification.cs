using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class PushNotification
    {
        [Key]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string subscrition { get; set; }
        //public MyProperty { get; set; }
    }
}
