using Domain.DTOs.Responses;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebPush;

namespace Domain.Interfaces
{
    public interface IPushNotificationRepo
    {
        public Task<bool> Save(string UserId, string subscription);
    }
    public interface IPushNotificationService
    {
        public Task<Result<bool>> Save(string UserId, string subscription);
        public Task SendPushNotification(PushSubscription subscription, string payload);
    }

    
}
