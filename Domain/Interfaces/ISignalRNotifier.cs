using Domain.DTOs.Responses;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ISignalRNotifier //calls repo methods within it
    {
        public Task<AuthRes> JoinQueueGroup(int eventId, string userId, string ConnectionId);
        public Task OnDisconnectedAsync(string ConnectionId);
        public Task OnConnectedAsync(string ConnectionId, string userId);
        public Task NotifyUserPositionChange(string userId, LineInfoRes lineInfoRes, string leaveQueueMessage="");
        public Task ExitQueue(string userId,long lineMemberId, string adminId= "", int position=-1, string leaveQueueMessage="");
        public Task ToggleQueueActivity(bool status, string userId, long eventId);  
        public Task NotifyUserQueueStatusUpdate(string userId, bool isQueueActive);


    }
    public interface ISignalRNotifierRepo // Repo methods needed for INotifier
    {
        public Task BroadcastLineUpdate(Line line,int position);

        public Task BroadcastLineActivity(long eventId, bool status);

    }
}
