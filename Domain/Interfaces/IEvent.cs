using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IEventRepo
    {

        public Task<bool> EditEvent(EditEventReq req);

        public Task<AuthRes> JoinEvent(string userId, long eventId);

        //public Task<bool> ServeLineMember(string userId, long eventId);

        public Task<Event> GetEvent(long Id);

        public Task<List<Event>> GetActiveEvents();


        public Task<EventQueueRes> GetEventQueue(int currentMembersPage, int pastMembersPage, int size, long eventId);

        public Task<GetUserEventsRes> GetUserEvents(string userId);

        public void DeleteEvent(long Id);

        public Task<bool> EventExists(string title);
        public Task AddEvent(Event evt);
        public Task<int> SaveChangesAsync();

        public Task<bool> ExitQueue(string userId, long lineMemberId, string adminId = "",int position=-1, string leaveQueueReason = "");

        public Task<bool> ToggleQueueActivity(bool status, string userId, long eventId);

        public Task<SearchEventsRes> SearchEvents(int page, int size, string query, string userId);

    }
    public interface IEventService
    {
        public Task<Result<bool>> CreateEvent(string userId, CreateEventModel req);

        //public Task<Result<LineMember>> JoinEvent(string userId, long eventId);

        public Task<Result<bool>> EditEvent(EditEventReq req);

        public Task<bool> EventExists(string title);
        public Task<Result<Event>> GetEvent(long eventId);

        public Task<Result<EventQueueRes>> GetEventQueue(int currentMembersPage, int pastMembersPage, int size, long eventId);
        public Task<Result<GetUserEventsRes>> GetUserEvents(string userId);
        public Result<bool> DeleteEvent(long Id);
        public Task<Result<SearchEventsRes>> SearchEvents(int page, int size, string query, string userId);

        public Task AddEvent(Event evt);
        public Task<int> SaveChangesAsync();
    }
}
