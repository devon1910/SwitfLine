using Azure.Core.GeoJson;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Infrastructure.Repositories
{
    public class SignalRNotifierRepo(SwiftLineDatabaseContext dbContext, 
        ILineRepo lineRepo, 
        ISignalRNotifier signalRNotifierHub,
        IPushNotificationService pushNotificationService) : ISignalRNotifierRepo
    {
        public async Task BroadcastLineActivity(long eventId, bool status)
        {
            var othersInLines = await dbContext.Lines
                .Where(x => !x.IsAttendedTo && x.IsActive)
                .Include(x => x.LineMember)
                .AsSplitQuery()
                .Where(x => x.LineMember.EventId == eventId)
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();       

            foreach (var line in othersInLines)
            {
                string userId = line.LineMember.UserId;
                await signalRNotifierHub.NotifyUserQueueStatusUpdate(userId, status);
            }

           
        }

        public async Task BroadcastLineUpdate(Line line, int position)
        {
            try
            {
                long eventId = line.LineMember.EventId;
                string currentUserId = line.LineMember.UserId;

                // Fetch all users in line for this event in a single query
                var usersInLine = await dbContext.Lines
                    .Where(x => !x.IsAttendedTo && x.IsActive && x.LineMember.EventId == eventId)
                    .Select(x => new
                    {
                        UserId = x.LineMember.UserId,
                        LineId = x.Id
                    })
                    .OrderBy(x => x.LineId)
                    .AsNoTracking()
                    .ToListAsync();

                List<string> userIds = usersInLine.Select(x => x.UserId).ToList();

                //get all subscibed users
                Dictionary<string, string> subscribedUsers = await dbContext.PushNotifications
                    .Where(x => userIds.Contains(x.UserId) || x.UserId == currentUserId)
                    .ToDictionaryAsync(x => x.UserId, x => x.subscrition);


                // Notify the current user first
                var currentUserLineInfo = await lineRepo.GetUserLineInfo(currentUserId);
                await signalRNotifierHub.NotifyUserPositionChange(currentUserId, currentUserLineInfo);
                subscribedUsers.TryGetValue(currentUserId, out var userSubscription1);
                if (userSubscription1 is not null)
                {
                    var message = JsonSerializer.Serialize(new
                    {
                        title = "Queue Update!",
                        body = $"You're now position {currentUserLineInfo.PositionRank} in the queue!"
                    });
                    var pushSubscription = JsonSerializer.Deserialize<SubscriptionModel>(userSubscription1);
                    await pushNotificationService.SendPushNotification(pushSubscription, message);
                }

                for (int i = 0; i < usersInLine.Count; i++)
                {
                    var user = usersInLine[i];
                    var lineInfo = await lineRepo.GetUserLineInfo(user.UserId);
                    string leaveQueueMessage = "";

                    if (position > -1 && position <= lineInfo.Position)
                    {
                        leaveQueueMessage = $"Line Member at the {GetOrdinal(position)} position has been served earlier than envisaged.";
                    }
                    await signalRNotifierHub.NotifyUserPositionChange(user.UserId, lineInfo, leaveQueueMessage);

                    //SendPush Notification
                    subscribedUsers.TryGetValue(user.UserId, out var userSubscription);
                    if (userSubscription is not null)
                    {
                        var message = JsonSerializer.Serialize(new
                        {
                            title = "Queue Update!",
                            body = $"You're now position {lineInfo.PositionRank} in the queue!"
                        });
                        var pushSubscription = JsonSerializer.Deserialize<SubscriptionModel>(userSubscription);
                        await pushNotificationService.SendPushNotification(pushSubscription, message);
                    }

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
            

            //var othersInLines = await dbContext.Lines
            //     .Where(x => !x.IsAttendedTo && x.IsActive && x.LineMember.EventId == line.LineMember.EventId)
            //     .Include(x => x.LineMember)
            //     .AsSplitQuery()
            //     .OrderBy(x => x.CreatedAt)
            //     .AsNoTracking()
            //     .ToListAsync();

            //var userId = line.LineMember.UserId;
            //var lineInfo = await lineRepo.GetUserLineInfo(userId);
            //await notifierHub.NotifyUserPositionChange(userId, lineInfo);

            //var @event = await dbContext.Events.FindAsync(line.LineMember.EventId);

            //foreach (var l in othersInLines)
            //{
            //    var otherUserId = l.LineMember.UserId;
            //    var otherLineInfo = await lineRepo.GetUserLineInfo(otherUserId);
            //    await notifierHub.NotifyUserPositionChange(otherUserId, otherLineInfo);
            //}

            //if (!othersInLines.Any() && @event != null && !@event.IsActive)
            //{
            //    await dbContext.Database.ExecuteSqlInterpolatedAsync(
            //  $"UPDATE public.\"Events\" set \"IsActive\"=\'true\' where \"Id\"={@event.Id}");
            //}

        }
        private static string GetOrdinal(int number)
        {
            int lastTwo = number % 100;
            if (lastTwo >= 11 && lastTwo <= 13) return "th";

            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th",
            };
        }

        public class Keys
        {
            public string p256dh { get; set; }
            public string auth { get; set; }
        }

        public class SubScriptionModel
        {
            public string endpoint { get; set; }
            public object expirationTime { get; set; }
            public Keys keys { get; set; }
        }


    }
}
