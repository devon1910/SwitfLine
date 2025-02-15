using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class EventService(IEventRepo eventRepo) : IEventService
    {
        public async Task<Result<bool>> CreateEvent(string userId, CreateEventReq req)
        {
            var createdEvent = await eventRepo.CreateEvent(userId, req);

            if (createdEvent)
            {
                return Result<bool>.Ok(true);
            }
            return Result<bool>.Failed("Failed to create event");
        }

        public async Task<Result<bool>> JoinEvent(string userId, long eventId)
        {
            bool isJoined = await eventRepo.JoinEvent(userId, eventId);

            if (isJoined)
            {
                return Result<bool>.Ok(true);
            }
            return Result<bool>.Failed("Failed to join event");
        }
    }
}
