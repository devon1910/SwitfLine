using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{

    public class EventRepo(SwiftLineDatabaseContext dbContext, ILineRepo lineRepo, INotifierRepo notifier) : IEventRepo 
    {
        public async Task<bool> CreateEvent(string userId, CreateEventModel req)
        {

            var newEvent = new Event
            {
                Title = req.Title,
                Description = req.Description,
                AverageTime = req.AverageTime,
                CreatedBy = userId,
                EventStartTime = TimeOnly.TryParse(req.EventStartTime, out _) ? TimeOnly.Parse(req.EventStartTime) : default,
                EventEndTime = TimeOnly.TryParse(req.EventEndTime, out _) ? TimeOnly.Parse(req.EventEndTime) : default

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

            @event.Title = req.Title;
            @event.AverageTime = req.AverageTime;
            @event.EventStartTime = TimeOnly.TryParse(req.EventStartTime, out _) ? TimeOnly.Parse(req.EventStartTime) : default;
            @event.EventEndTime = TimeOnly.TryParse(req.EventEndTime, out _) ? TimeOnly.Parse(req.EventEndTime) : default;
            @event.Description = req.Description;
            await dbContext.SaveChangesAsync();
            return true;

        }

        public async Task<List<Event>> GetActiveEvents()
        {
            var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));

            var unfinishedEvents = await dbContext.Lines.Where(x => !x.IsAttendedTo).Include(x => x.LineMember).Select(x => x.LineMember.EventId).ToListAsync();
            return await dbContext.Events
                .AsNoTracking()
                .Where(x => x.IsActive &&
                        (
                            // For events that do not span midnight:
                            x.EventStartTime <= x.EventEndTime
                            ? (timeNow >= x.EventStartTime && timeNow <= x.EventEndTime)
                            // For events that span midnight:
                            : (timeNow >= x.EventStartTime || timeNow <= x.EventEndTime)
                        ) || unfinishedEvents.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<Event>> GetAllEvents()
        {
            List<Event> events = await dbContext.Events.Where(x => x.IsActive).ToListAsync();
            foreach (var @event in events)
            {
                @event.UsersInQueue = await dbContext.Lines
                    .Include(x => x.LineMember)
                    .Where(x => x.LineMember.EventId == @event.Id && !x.IsAttendedTo).CountAsync();
                var user = await dbContext.SwiftLineUsers.FindAsync(@event.CreatedBy);
                @event.CreatedBy = user.Email;
            }
            return events;
        }

        public async Task<Event> GetEvent(long eventId)
        {
            return await dbContext.Events.FindAsync(eventId);
        }

        public async Task<List<Line>> GetEventQueue(long eventId)
        {
            var lines = await dbContext.Lines
                        .Where(x => !x.IsAttendedTo)
                        .Include(x => x.LineMember)
                        .ThenInclude(x => x.SwiftLineUser)
                        .Where(x => x.LineMember.EventId == eventId)
                        .ToListAsync();
            return lines;

        }

        public async Task<long> JoinEvent(string userId, long eventId)
        {
            if (await isUserInLine(userId)) return 0;

            LineMember newQueueMember = new LineMember
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

            SwiftLineUser user = await dbContext.SwiftLineUsers.FindAsync(userId);
            user.IsInQueue = true;
            await dbContext.SaveChangesAsync();
            return newQueueMember.Id;

        }
        public void DeleteEvent(long Id)
        {
            Event eventToDelete = dbContext.Events.Find(Id);
            dbContext.Events.Remove(eventToDelete);
            dbContext.SaveChanges();
        }

        private async Task<bool> isUserInLine(string userId)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);

            return user.IsInQueue;
        }

        public async Task<List<Event>> GetUserEvents(string userId)
        {
            return await dbContext.Events.Where(x => x.CreatedBy == userId).ToListAsync();
        }

        public async Task<bool> ExitQueue(string userId, long lineMemberId, string adminId = "")
        {
            Line line = dbContext.Lines.FirstOrDefault(x => x.LineMemberId == lineMemberId);

            await lineRepo.MarkUserAsAttendedTo(line, "");
            await notifier.BroadcastLineUpdate(line);
            await lineRepo.NotifyFifthMember(line);
            return true;
        }

    }
}
