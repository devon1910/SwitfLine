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
            return Result<bool>.Failed("Failed to create event");
        }

        public async Task<Result<LineInfoRes>> JoinEvent(string userId, long eventId)
        {
            var joinedRes = await eventRepo.JoinEvent(userId, eventId);

            return Result<LineInfoRes>.Ok(joinedRes);
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

        public async Task<Result<List<Event>>> GetAllEvents()
        {
            var allEvents = await eventRepo.GetAllEvents();

            return Result<List<Event>>.Ok(allEvents);
        }

        public async Task<Result<List<Line>>> GetEventQueue(long eventId)
        {
            var lines = await  eventRepo.GetEventQueue(eventId);
            return Result<List<Line>>.Ok(lines);
        }

    }
}
