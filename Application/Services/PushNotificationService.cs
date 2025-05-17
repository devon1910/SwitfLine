using Domain.DTOs.Responses;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebPush;

namespace Application.Services
{
    public class PushNotificationService(
        IPushNotificationRepo pushNotificationRepo,
        IConfiguration config) : IPushNotificationService
    {
        public async Task<Result<bool>> Save(string UserId, string subscription)
        {
            var result = await pushNotificationRepo.Save(UserId, subscription);

            if (result) return Result<bool>.Ok(result);

            else return Result<bool>.Failed("Failed to save subscription");

        }

        public async Task SendPushNotification(PushSubscription subscription, string payload)
        {
            var webPushClient = new WebPushClient();

            var vapidDetails = new VapidDetails(
                "mailto:Swiftline00@gmail.com", 
                config["Vapid_PublicKey"], 
                config["Vapid_PrivateKey"]);

            await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
        }

      
    }
}
