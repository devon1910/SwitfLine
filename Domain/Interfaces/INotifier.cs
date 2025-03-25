using Domain.DTOs.Responses;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface INotifier //calls repo methods within it
    {
        public Task<long> JoinQueueGroup(int eventId, string userId, string ConnectionId);
        public Task OnDisconnectedAsync(string ConnectionId);
        public Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes);
        public Task ExitQueue(string userId,long lineMemberId, string adminId= "");
        public Task ToggleQueueActivity(bool status, string userId, long eventId);  
        public Task NotifyUserQueueStatusUpdate(string userId, bool isQueueActive);


    }
    public interface INotifierRepo // Repo methods needed for INotifier
    {
        public Task BroadcastLineUpdate(Line line);

        public Task BroadcastLineActivity(Line line, bool status);

    }
}
