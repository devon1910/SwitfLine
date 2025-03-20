using Application.Services;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SwiftLine.API
{
    public class SwiftLineHub(INotifier notifier) : Hub
    {

        public async Task<long> JoinQueueGroup(int eventId, string userId)
        {
            return await notifier.JoinQueueGroup(eventId, userId, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove from user connections mapping
            await notifier.OnDisconnectedAsync(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

       
        public async Task BroadcastQueueUpdate(string eventId, object queueUpdate)
        {
            await Clients.Group($"queue-{eventId}").SendAsync("ReceiveQueueUpdate", queueUpdate);
        }

        public async Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes)
        {
            await notifier.NotifyUserPositionChange(userId, lineInfoRes);     
        }

        public async Task ExitQueue(string userId, long lineMemberId, string adminId = "")
        {
            await notifier.ExitQueue(userId, lineMemberId, adminId);
        }
    }
}
