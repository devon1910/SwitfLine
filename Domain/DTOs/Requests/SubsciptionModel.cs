using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public class Keys
    {
        public string p256dh { get; set; }
        public string auth { get; set; }
    }

    public class SubscriptionModel
    {
        public string endpoint { get; set; }
        public object expirationTime { get; set; }
        public Keys keys { get; set; }
    }

}
