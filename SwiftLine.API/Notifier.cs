using Domain.DTOs.Responses;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SwiftLine.API
{
    public class Notifier(IHubContext<SwiftLineHub> _hubContext, Lazy<IEventRepo> eventRepo) : INotifier
    {
        private static Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        public async Task ExitQueue(string userId, long lineMemberId, string adminId = "")
        {
             await eventRepo.Value.ExitQueue(userId,lineMemberId,adminId);

        }

        public async Task<long> JoinQueueGroup(int eventId, string userId, string ConnectionId)
        {
            await _hubContext.Groups.AddToGroupAsync(ConnectionId, $"queue-{eventId}");

            _userConnections[userId] = ConnectionId;

            return await eventRepo.Value.JoinEvent(userId, eventId);

        }

        public async Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes)
        {
            if (_userConnections.TryGetValue(userId, out string connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceivePositionUpdate", lineInfoRes);
            }
        }
        public async Task SendSingleUserMessage(string userId, long LineMemberId)
        {
            if (_userConnections.TryGetValue(userId, out string connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLineMemberId", LineMemberId);
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
