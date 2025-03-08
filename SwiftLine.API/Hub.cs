using Domain.DTOs.Responses;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class SwiftLineHub : Hub
    {
        // Store connection IDs mapped to user IDs for direct messaging
        private static Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        // Join a specific queue group
        public async Task JoinQueueGroup(string eventId, string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{eventId}");

            _userConnections[userId] = Context.ConnectionId;

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
