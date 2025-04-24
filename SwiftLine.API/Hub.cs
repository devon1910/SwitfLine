using Application.Services;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SwiftLine.API
{
    [Authorize]
    public class SwiftLineHub(INotifier notifier) : Hub
    {
        [EnableRateLimiting("SignupPolicy")]
        public async Task<long> JoinQueueGroup(int eventId, string userId)
        {
            Log.Information("User {UserId} joining queue group for event {EventId}", userId, eventId);
            try
            {
                //var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await notifier.JoinQueueGroup(eventId, userId, Context.ConnectionId);
                Log.Information("User {UserId} successfully joined queue group for event {EventId}", userId, eventId);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error joining queue group for user {UserId} and event {EventId}", userId, eventId);
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Log.Information("Client disconnected. ConnectionId: {ConnectionId}", Context.ConnectionId);
            try
            {
                await notifier.OnDisconnectedAsync(Context.ConnectionId);
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling disconnection for connection {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

       
        public async Task BroadcastQueueUpdate(string eventId, object queueUpdate)
        {
            Log.Debug("Broadcasting queue update for event {EventId}", eventId);
            await Clients.Group($"queue-{eventId}").SendAsync("ReceiveQueueUpdate", queueUpdate);
        }

        public async Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes)
        {
            Log.Information("Notifying user {UserId} of position change", userId);
            try
            {
                await notifier.NotifyUserPositionChange(userId, lineInfoRes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error notifying user {UserId} of position change", userId);
                throw;
            }
        }

        public async Task ExitQueue(string userId, long lineMemberId, string adminId = "")
        {
            Log.Information("User {UserId} exiting queue. LineMemberId: {LineMemberId}, AdminId: {AdminId}", 
                userId, lineMemberId, adminId);
            try
            {
                await notifier.ExitQueue(userId, lineMemberId, adminId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exiting queue for user {UserId}, LineMemberId: {LineMemberId}", 
                    userId, lineMemberId);
                throw;
            }
        }

        public async Task ToggleQueueActivity(bool status, string userId, long eventId) 
        {
            Log.Information("Toggling queue activity. Status: {Status}, UserId: {UserId}, EventId: {EventId}", 
                status, userId, eventId);
            try
            {
                await notifier.ToggleQueueActivity(status, userId, eventId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error toggling queue activity. Status: {Status}, UserId: {UserId}, EventId: {EventId}", 
                    status, userId, eventId);
                throw;
            }
        }
    }
}
