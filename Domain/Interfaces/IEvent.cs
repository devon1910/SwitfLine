using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IEventRepo
    {
        public Task<bool> CreateEvent(string userId,CreateEventReq req);

        public Task<bool> JoinEvent(string userId, long eventId);

    }
    public interface IEventService
    {
        public Task<Result<bool>> CreateEvent(string userId, CreateEventReq req);

        public Task<Result<bool>> JoinEvent(string userId, long eventId);

    }
}
