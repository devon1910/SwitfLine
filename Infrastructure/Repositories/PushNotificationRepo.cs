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
                if (existingSubscription.Subscrition != subscription) 
                {
                    existingSubscription.Subscrition = subscription;
                    existingSubscription.DateLastUpdated = DateTime.UtcNow.AddHours(1);
                    await databaseContext.SaveChangesAsync();
                } 
            }
            else 
            {
                await databaseContext.PushNotifications.AddAsync(new PushNotification
                {
                    Subscrition = subscription,
                    UserId = UserId,
                    DateLastUpdated = DateTime.UtcNow.AddHours(1)
                });
                await databaseContext.SaveChangesAsync();
            }
         
            return true;
        }
    }
}
