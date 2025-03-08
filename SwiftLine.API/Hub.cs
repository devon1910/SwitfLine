using Domain.DTOs.Responses;
using Domain.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Infrastructure
{
    public class SwiftLineHub : Hub
    {
        // Store connection IDs mapped to user IDs for direct messaging
        private static Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        // Join a specific queue group
        public async Task JoinQueueGroup(int eventId, string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{eventId}");

            _userConnections[userId] = Context.ConnectionId;

            //get the position of the user in the queue
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveLineInfo", new { position=1, timeTillYourTurn=2, eventId=3, lineMemberId=4 });

            Console.WriteLine($"User {userId} joined queue for event {eventId}");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove from user connections mapping
            string userId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.Remove(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        //public async Task ProcessQueueMovement(string eventId, Dictionary<string, object> userUpdates)
        //{
        //    // First broadcast general update to everyone
        //    await BroadcastQueueUpdate(eventId, new { message = "The First person has been served." });

        //    // Then send personalized updates to each affected user
        //    foreach (var update in userUpdates)
        //    {
        //        string userId = update.Key;
        //        object personalData = update.Value;

        //        await NotifyUserPositionChange(userId, personalData);
        //    }
        //}
        // Broadcast to all clients in a queue (called from your queue service)
        public async Task BroadcastQueueUpdate(string eventId, object queueUpdate)
        {
            await Clients.Group($"queue-{eventId}").SendAsync("ReceiveQueueUpdate", queueUpdate);
        }

        public async Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes)
        {
            if (_userConnections.TryGetValue(userId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceivePersonalUpdate", lineInfoRes);
            }
        }



    }
}
