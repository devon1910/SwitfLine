using Domain.DTOs.Responses;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface INotifier
    {
        public Task JoinQueueGroup(int eventId, string userId, string ConnectionId);
        public Task OnDisconnectedAsync(string ConnectionId);
        public Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes);


    }

    public interface INotifierRepo
    {
        public Task BroadcastLineUpdate(Line line);
    }
}
