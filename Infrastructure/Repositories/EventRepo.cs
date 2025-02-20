using Domain.DTOs.Requests;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    
    public class EventRepo(SwiftLineDatabaseContext dbContext) : IEventRepo
    {
        public async Task<bool> CreateEvent(string userId, CreateEventReq req)
        {
            
            var newEvent = new Event
            {
                Name = req.Name,
                AverageTimeToServeMinutes = req.AverageTimeToServe,
                CreatedBy = userId,
                EventStartTime = TimeOnly.TryParse(req.StartTime, out _) ? TimeOnly.Parse(req.StartTime) : default,
                EventEndTime = TimeOnly.TryParse(req.EndTime, out _) ? TimeOnly.Parse(req.EndTime) : default

            };
            await dbContext.Events.AddAsync(newEvent);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditEvent(EditEventReq req)
        {
            Event? @event = await dbContext.Events.FindAsync(req.EventId);

            if (@event == null)
            {
                return false;
            }

            @event.Name = req.Name;
            @event.AverageTimeToServeMinutes = req.AverageTimeToServe;
            await dbContext.SaveChangesAsync();
            return true;

        }

        public async Task<List<Event>> GetActiveEvents()
        {
            var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));


            return await dbContext.Events
                    .AsNoTracking()
                    .Where(x => x.IsActive &&
                           (
                             // For events that do not span midnight:
                             x.EventStartTime <= x.EventEndTime
                                ? (timeNow >= x.EventStartTime && timeNow <= x.EventEndTime)
                                // For events that span midnight:
                                : (timeNow >= x.EventStartTime || timeNow <= x.EventEndTime)
                           ))
                    .ToListAsync();
        }

        public async Task<Event> GetEvent(long eventId)
        {
            return await dbContext.Events.FindAsync(eventId);
        }

        public async Task<bool> JoinEvent(string userId, long eventId)
        {
            var newQueueMember = new LineMember
            {
                EventId = eventId,
                UserId = userId
            };

            await dbContext.LineMembers.AddAsync(newQueueMember);
            await dbContext.SaveChangesAsync();

            Line queue = new()
            {
                LineMemberId = newQueueMember.Id
            };

            await dbContext.Lines.AddAsync(queue);
            await dbContext.SaveChangesAsync();
            return true;
        }

      
       
    }
}
