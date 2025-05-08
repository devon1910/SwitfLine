using Domain.DTOs.Responses;
using Domain.Interfaces;
using Infrastructure.Repositories;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SwiftLine.API
{
    public class Notifier(IHubContext<SwiftLineHub> _hubContext, Lazy<IEventRepo> eventRepo) : INotifier
    {
        private static Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        public async Task ExitQueue(string userId, long lineMemberId, string adminId = "")
        {
            Log.Information("Processing queue exit for user {UserId}, LineMemberId: {LineMemberId}, AdminId: {AdminId}", userId, lineMemberId, adminId);
            try
            {
                await eventRepo.Value.ExitQueue(userId, lineMemberId, adminId);
                Log.Information("Successfully processed queue exit for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing queue exit for user {UserId}, LineMemberId: {LineMemberId}", userId, lineMemberId);
                throw;
            }
        }

        public async Task<AuthRes> JoinQueueGroup(int eventId, string userId, string ConnectionId)
        {
            Log.Information("Processing queue join for user {UserId}, EventId: {EventId}", userId, eventId);
            try
            {       
                var result = await eventRepo.Value.JoinEvent(userId, eventId);
                if (result.status) 
                {
                    Log.Information("Successfully processed queue join for user {UserId}, EventId: {EventId}", userId, eventId);
                    await _hubContext.Groups.AddToGroupAsync(ConnectionId, $"queue-{eventId}");
                    userId = string.IsNullOrEmpty(userId) ? result.userId : userId;
                    _userConnections[userId] = ConnectionId;
                }
                else
                {
                    Log.Warning("Failed to join queue for user {UserId}, EventId: {EventId}", userId, eventId);
                }              
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing queue join for user {UserId}, EventId: {EventId}", userId, eventId);
                throw;
            }
        }

        public async Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes)
        {
            Log.Debug("Attempting to notify user {UserId} of position change", userId);
            try
            {
                if (_userConnections.TryGetValue(userId, out string connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceivePositionUpdate", lineInfoRes);
                    Log.Debug("Successfully notified user {UserId} of position change", userId);
                }
                else
                {
                    Log.Warning("No active connection found for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error notifying user {UserId} of position change", userId);
                throw;
            }
        }

        public async Task OnDisconnectedAsync(string ConnectionId)
        {
            Log.Debug("Processing disconnection for connection {ConnectionId}", ConnectionId);
            try
            {
                string userId = _userConnections.FirstOrDefault(x => x.Value == ConnectionId).Key;
                if (!string.IsNullOrEmpty(userId))
                {
                    _userConnections.Remove(userId);
                    Log.Debug("Removed connection mapping for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing disconnection for connection {ConnectionId}", ConnectionId);
                throw;
            }
        }

        public async Task NotifyUserQueueStatusUpdate(string userId, bool isQueueActive)
        {
            Log.Debug("Attempting to notify user {UserId} of queue status update: {Status}", userId, isQueueActive);
            try
            {
                if (_userConnections.TryGetValue(userId, out string connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveQueueStatusUpdate", isQueueActive);
                    Log.Debug("Successfully notified user {UserId} of queue status update", userId);
                }
                else
                {
                    Log.Warning("No active connection found for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error notifying user {UserId} of queue status update", userId);
                throw;
            }
        }

        public async Task ToggleQueueActivity(bool status, string userId, long eventId)
        {
            Log.Information("Processing queue activity toggle. Status: {Status}, UserId: {UserId}, EventId: {EventId}", status, userId, eventId);
            try
            {
                await eventRepo.Value.ToggleQueueActivity(status, userId, eventId);
                Log.Information("Successfully toggled queue activity for user {UserId}, EventId: {EventId}", userId, eventId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error toggling queue activity for user {UserId}, EventId: {EventId}", userId, eventId);
                throw;
            }
        }

        public async Task OnConnectedAsync(string ConnectionId, string userId)
        {

            _userConnections[userId] = ConnectionId;
            Log.Debug("Set up new connection mapping for user {UserId}", userId);

        }
    }
}
