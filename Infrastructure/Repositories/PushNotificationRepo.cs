using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PushNotificationRepo(SwiftLineDatabaseContext databaseContext) : IPushNotificationRepo
    {
        public async Task<bool> Save(string UserId, string subscription)
        {
            var existingSubscription = await databaseContext.PushNotifications
                .FirstOrDefaultAsync(x => x.UserId == UserId);

            if (existingSubscription != null)
            {
                existingSubscription.subscrition = subscription;
            }
            else 
            {
                await databaseContext.PushNotifications.AddAsync(new PushNotification
                {
                    subscrition = subscription,
                    UserId = UserId
                });
            }
          
            await databaseContext.SaveChangesAsync();
            return true;
            //var test = databaseContext.Feedbacks.ExecuteUpdate(
            //    f => f.SetProperty(x => x.Comment, "test")
            //);
        }
    }
}
