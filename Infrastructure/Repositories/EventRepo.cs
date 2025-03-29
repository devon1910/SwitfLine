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
            Event existingEvent= dbContext.Events.FirstOrDefault(x => x.Title == req.Title);

            if (existingEvent != null)
            {
                return false;
            }
            var newEvent = new Event
            {
                Title = req.Title,
                Description = req.Description,
                AverageTime = req.AverageTime,
                AverageTimeToServeSeconds = req.AverageTime * 60,
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
            @event.AverageTimeToServeSeconds = req.AverageTime * 60;
            @event.EventStartTime = TimeOnly.TryParse(req.EventStartTime, out _) ? TimeOnly.Parse(req.EventStartTime) : default;
            @event.EventEndTime = TimeOnly.TryParse(req.EventEndTime, out _) ? TimeOnly.Parse(req.EventEndTime) : default;
            @event.Description = req.Description;
            await dbContext.SaveChangesAsync();
            return true;

        }

        public async Task<List<Event>> GetActiveEvents()
        {
            var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));

            var unfinishedEvents = await dbContext.Lines
                .AsNoTracking()
                .Where(x => !x.IsAttendedTo)
                .Include(x => x.LineMember)
                .ThenInclude(x => x.Event)
                .AsSplitQuery()
                .Where(x => x.LineMember.Event.IsActive)
                .Select(x => x.LineMember.EventId)            
                .ToListAsync();

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
            return await dbContext.Events.Include(x => x.SwiftLineUser).FirstOrDefaultAsync(x=>x.Id ==eventId);
        }

        public async Task<EventQueueRes> GetEventQueue(long eventId)
        {
            var lines = await dbContext.Lines
                        .AsNoTracking()
                        .Where(x => !x.IsAttendedTo)
                        .Include(x => x.LineMember)
                        .ThenInclude(x => x.SwiftLineUser)
                        .Include(x => x.LineMember).ThenInclude(x => x.Event)
                        .Where(x => x.LineMember.EventId == eventId)
                        .OrderBy(x=>x.CreatedAt)
                        .Select(x => new Line
                        {
                            Id = x.Id,
                            CreatedAt = x.CreatedAt.AddHours(-1),
                            LineMemberId = x.LineMemberId,
                            LineMember = new LineMember()
                            {
                                Id = x.LineMemberId,
                                UserId =x.LineMember.UserId,
                                SwiftLineUser = new SwiftLineUser
                                {
                                    Email = x.LineMember.SwiftLineUser.Email
                                }
                            },

                        }).ToListAsync();
            Event @event =  dbContext.Events.AsNoTracking().FirstOrDefault(x=>x.Id==eventId);


            return new EventQueueRes(lines,!@event.IsActive);

        }

        public async Task<long> JoinEvent(string userId, long eventId)
        {
            if (string.IsNullOrWhiteSpace(userId) || await isUserInLine(userId)) return 0;

            //event is active rn
            if (!isEventActiveRightNow(eventId)) return -1;

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

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE public.\"Events\" set \"UsersInQueue\"=\"UsersInQueue\" + 1 where \"Id\"={eventId}");

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
            Line line = dbContext.Lines
                .Where(x => x.LineMemberId == lineMemberId)
                .Include(x=>x.LineMember)
                .FirstOrDefault();

            await lineRepo.MarkUserAsAttendedTo(line, "");
            await notifier.BroadcastLineUpdate(line);
            await lineRepo.NotifyFifthMember(line);
            return true;
        }

        public async Task<bool> ToggleQueueActivity(bool status, string userId, long eventId)
        {
            Event @event = dbContext.Events.Find(eventId);
            @event.IsActive = status;
            await dbContext.SaveChangesAsync();
            await notifier.BroadcastLineActivity(eventId, status);
            return true;
        }

        public async Task<SearchEventsRes> SearchEvents(int page, int size, string query, string userId)
        {

            var allEvents =  dbContext.Events.AsQueryable();

            int pageCount = (allEvents.Count() + size - 1) / size;

            if (string.IsNullOrEmpty(query))
            {

                var eventsData = await allEvents
                 .Where(x => x.IsActive)
                 .OrderBy(x => x.EventStartTime)
                 .Skip((page - 1) * size)
                 .Take(size)
                 .Include(x => x.SwiftLineUser)
                 .ToListAsync();

                var events = eventsData.Select(x => new Event()
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    AverageTime = x.AverageTime,
                    EventStartTime = x.EventStartTime,
                    EventEndTime = x.EventEndTime,
                    UsersInQueue = x.UsersInQueue,
                    Organizer = x.SwiftLineUser.UserName,
                    IsOngoing = isEventActiveRightNow(x)
                }).ToList();
                return new SearchEventsRes ( events, pageCount, GetUserQueueStatus(userId));
            }

            var searchEventsData = await allEvents
                 .Where(x => x.IsActive && x.Title.Contains(query))
                 .OrderBy(x => x.EventStartTime)
                 .Skip((page - 1) * size)
                 .Take(size)
                 .Include(x => x.SwiftLineUser)
                 .ToListAsync();

            var searchEvents = searchEventsData.Select(x => new Event()
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                AverageTime = x.AverageTime,
                EventStartTime = x.EventStartTime,
                EventEndTime = x.EventEndTime,
                UsersInQueue = x.UsersInQueue,
                Organizer = x.SwiftLineUser.UserName,
                IsOngoing = isEventActiveRightNow(x)
            }).ToList();
            return new SearchEventsRes(searchEvents, pageCount, GetUserQueueStatus(userId));
        }

        private bool GetUserQueueStatus(string UserId)
        {
            var user = dbContext.SwiftLineUsers.Find(UserId);
            return user is not null ? user.IsInQueue : false;

        }

        private bool isEventActiveRightNow(long eventId)
        {

            var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));
            Event @event = dbContext.Events.Find(eventId);

            if (@event.EventStartTime <= @event.EventEndTime)
            {
                return timeNow >= @event.EventStartTime && timeNow <= @event.EventEndTime;
            }
            else
            {
                return timeNow >= @event.EventStartTime || timeNow <= @event.EventEndTime;
            }
        }
        
        private bool isEventActiveRightNow(Event @event)
        {

            var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));

            if (@event.EventStartTime <= @event.EventEndTime)
            {
                return timeNow >= @event.EventStartTime && timeNow <= @event.EventEndTime;
            }
            else
            {
                return timeNow >= @event.EventStartTime || timeNow <= @event.EventEndTime;
            }
        }
    }
}
