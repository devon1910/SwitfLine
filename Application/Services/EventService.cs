using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
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
        public async Task<Result<bool>> CreateEvent(string userId, CreateEventModel req)
        {
            var createdEvent = await eventRepo.CreateEvent(userId, req);

            if (createdEvent)
            {
                return Result<bool>.Ok(true);
            }
            return Result<bool>.Failed("Event title exists already. Please use a different event title and try again.");
        }


        public async Task<Result<bool>> EditEvent(EditEventReq req)
        {
            bool isEdited = await eventRepo.EditEvent(req);
            if (isEdited)
            {
                return Result<bool>.Ok(true);
            }
            return Result<bool>.Failed("Failed to edit event");

        }

        public async Task<Result<Event>> GetEvent(long eventId)
        {
            var @event = await eventRepo.GetEvent(eventId);
            if (@event is null)
            {
                return Result<Event>.Failed("Event not found");
            }
            return Result<Event>.Ok(@event);
        }


        public async Task<Result<EventQueueRes>> GetEventQueue(int page, int size, long eventId, bool isForPastMembers = false)
        {
            var res = await  eventRepo.GetEventQueue(page, size, eventId,isForPastMembers);
            return Result<EventQueueRes>.Ok(res);
        }

        public async Task<Result<List<Event>>> GetUserEvents(string userId)
        {
            var userEvents = await eventRepo.GetUserEvents(userId);

            return Result<List<Event>>.Ok(userEvents);
        }

        public Result<bool> DeleteEvent(long Id)
        {
            eventRepo.DeleteEvent(Id);
            return Result<bool>.Ok(true);
        }

        public async Task<Result<SearchEventsRes>> SearchEvents(int page, int size, string searchQuery, string userId)
        {
            var result = await eventRepo.SearchEvents(page, size, searchQuery, userId);

            return Result<SearchEventsRes>.Ok(result);
        }
    }
}
