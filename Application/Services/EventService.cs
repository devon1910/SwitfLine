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

            if (await EventExists(req.Title))
            {
                return Result<bool>.Failed("Event title exists already. Please use a different event title and try again.");
            }

            var newEvent = new Event
            {
                Title = req.Title,
                Description = req.Description,
                AverageTime = req.AverageTime,
                AverageTimeToServeSeconds = req.AverageTime * 60,
                CreatedBy = userId,
                EventStartTime = TimeOnly.TryParse(req.EventStartTime, out _) ? TimeOnly.Parse(req.EventStartTime) : default,
                EventEndTime = TimeOnly.TryParse(req.EventEndTime, out _) ? TimeOnly.Parse(req.EventEndTime) : default,
                StaffCount = req.StaffCount,
                Capacity = req.Capacity,
                AllowAnonymousJoining = req.AllowAnonymousJoining,

            };

            await eventRepo.AddEvent(newEvent);
            await eventRepo.SaveChangesAsync();
            return Result<bool>.Ok(true,"New Event Created");

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


        public async Task<Result<EventQueueRes>> GetEventQueue(int currentMembersPage,int pastMembersPage, int size, long eventId)
        {
            var res = await  eventRepo.GetEventQueue(currentMembersPage, pastMembersPage, size, eventId);
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

        public async Task<bool> EventExists(string title)
        {
            return await eventRepo.EventExists(title);
        }

        public async Task AddEvent(Event evt)
        {
            await eventRepo.AddEvent(evt);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await eventRepo.SaveChangesAsync();
        }
    }
}
