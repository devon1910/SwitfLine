using Domain.DTOs.Responses;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SwiftLine.API
{
    public class Notifier(IHubContext<SwiftLineHub> _hubContext, IEventRepo eventRepo) : INotifier
    {
        private static Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        public async Task JoinQueueGroup(int eventId, string userId, string ConnectionId)
        {
            await _hubContext.Groups.AddToGroupAsync(ConnectionId, $"queue-{eventId}");

            _userConnections[userId] = ConnectionId;

            await eventRepo.JoinEvent(userId, eventId);
            //get the position of the user in the queue
            await _hubContext.Clients.Client(ConnectionId).SendAsync("ReceiveIsInQueueUpdate", true);

            Console.WriteLine($"User {userId} joined queue for event {eventId}");
        }

        public async Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes)
        {
            if (_userConnections.TryGetValue(userId, out string connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceivePositionUpdate", lineInfoRes);
            }
        }

       

        public async Task OnDisconnectedAsync(string ConnectionId)
        {
            string userId = _userConnections.FirstOrDefault(x => x.Value == ConnectionId).Key;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.Remove(userId);
            }
        }
    }
}
