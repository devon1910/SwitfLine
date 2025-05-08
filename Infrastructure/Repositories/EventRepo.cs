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
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Infrastructure.Repositories
{

    public class EventRepo(SwiftLineDatabaseContext dbContext, ILineRepo lineRepo, INotifierRepo notifier, IAuthRepo authRepo) : IEventRepo 
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
                EventEndTime = TimeOnly.TryParse(req.EventEndTime, out _) ? TimeOnly.Parse(req.EventEndTime) : default,
                StaffCount = req.StaffCount,
                Capacity = req.Capacity,
                AllowAnonymousJoining = req.AllowAnonymousJoining,

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

            //@event.Title = req.Title;
            @event.AverageTime = req.AverageTime;
            @event.AverageTimeToServeSeconds = req.AverageTime * 60;
            @event.EventStartTime = TimeOnly.TryParse(req.EventStartTime, out _) ? TimeOnly.Parse(req.EventStartTime) : default;
            @event.EventEndTime = TimeOnly.TryParse(req.EventEndTime, out _) ? TimeOnly.Parse(req.EventEndTime) : default;
            @event.Description = req.Description;
            @event.StaffCount = req.StaffCount;
            @event.Capacity = req.Capacity;
            @event.AllowAnonymousJoining = req.AllowAnonymousJoining;
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
                .Where(x => x.LineMember.Event.IsActive && !x.LineMember.Event.IsDeleted)
                .Select(x => x.LineMember.EventId)            
                .ToListAsync();

            return await dbContext.Events
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted &&
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
            List<Event> events = await dbContext.Events.Where(x => x.IsActive && !x.IsDeleted).ToListAsync();
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
            return await dbContext.Events.Include(x => x.SwiftLineUser).FirstOrDefaultAsync(x=>x.Id ==eventId && !x.IsDeleted);
        }

        public async Task<EventQueueRes> GetEventQueue(int page, int size, long eventId, bool isForPastMembers = false)
        {
            var allLines = dbContext.Lines
                .Where(x => x.LineMember.EventId == eventId)
                .Include(x => x.LineMember)
                .AsNoTracking();

            var allIndividualsInQueue = allLines.Where(x => (isForPastMembers ? x.IsAttendedTo : !x.IsAttendedTo)).Count();

            int pageCount = (int) Math.Ceiling(allIndividualsInQueue / (double) size);

            var lines = await allLines
               .Where(x => (isForPastMembers ? x.IsAttendedTo : !x.IsAttendedTo))        
               .Take(size)
               .Select(x => new Line
               {
                   Id = x.Id,
                   CreatedAt = x.CreatedAt.AddHours(-1),
                   LineMemberId = x.LineMemberId,
                   DateCompletedBeingAttendedTo = x.DateCompletedBeingAttendedTo.AddHours(-1),
                   DateStartedBeingAttendedTo = x.DateStartedBeingAttendedTo.AddHours(-1),
                   IsAttendedTo = x.IsAttendedTo,
                   Status = x.Status,
                   TimeWaited = x.TimeWaited,
                   LineMember = new LineMember
                   {
                       Id = x.LineMemberId,
                       UserId = x.LineMember.UserId,
                       SwiftLineUser = new SwiftLineUser
                       {
                           UserName = x.LineMember.SwiftLineUser.UserName
                       }
                   }
               }).ToListAsync();

            var isEventActive =  dbContext.Events.Find(eventId).IsActive;

            var TotalServed = allLines.Where(x => x.LineMember.EventId == eventId && x.Status.Contains("served")).Count();

            int averageTime = 0;

            if (allLines.Count() > 0) 
            {
                 averageTime = (int)Math.Ceiling(allLines.Select(x => x.TimeWaited).Average());
            }

            lines = isForPastMembers ? [.. lines.OrderByDescending(x => x.CreatedAt)] : [.. lines.OrderBy(x => x.CreatedAt)];
            lines = lines.Skip((page - 1) * size).Take(size).ToList();


            return new EventQueueRes(lines, !isEventActive, pageCount, TotalServed, averageTime);

        }

        public async Task<AuthRes> JoinEvent(string userId, long eventId)
        {
            var Event= await dbContext.Events.FindAsync(eventId);

            if (Event is null || Event.IsDeleted) 
            {
                return AuthResFailed.CreateFailed("Event not found");
            }

            if (!isEventActiveRightNow(Event)) 
            {
                return AuthResFailed.CreateFailed("Event hasn't started.");
            }

            var user = await getUser(userId);
            AnonymousUserAuthRes creationResult = null;


            if (user is null)
            {
                if (!Event.AllowAnonymousJoining)
                {
                    const string errorMessage = "The Event Organizer has disabled anonymous joining. " +
                                               "Please login or sign up to join this queue";
                    return AuthResFailed.CreateFailed(errorMessage);
                }

                creationResult = await authRepo.CreateAnonymousUser();
                if (!creationResult.status)
                {
                    const string errorMessage = "Unable to create an anonymous account. " +
                                               "Please try again later";
                    return AuthResFailed.CreateFailed(errorMessage);
                }

                user = creationResult.user;
            }

            string token = creationResult?.AccessToken ?? string.Empty;

            LineMember newQueueMember = new LineMember
            {
                EventId = eventId,
                UserId = string.IsNullOrEmpty(userId) ? creationResult.user.Id : userId,
            };

            await dbContext.LineMembers.AddAsync(newQueueMember);
            await dbContext.SaveChangesAsync();

            Line queue = new()
            {
                LineMemberId = newQueueMember.Id
            };
            await dbContext.Lines.AddAsync(queue);

            user.IsInQueue = true;
            user.LastEventJoined = eventId;

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE public.\"Events\" set \"UsersInQueue\"=\"UsersInQueue\" + 1 where \"Id\"={eventId}");

            await dbContext.SaveChangesAsync();
            return new AuthRes(true, "Joined queue Successfully", token,
                "", user.Id, user.Email, user.UserName);

        }

       
        public void DeleteEvent(long Id)
        {
            Event eventToDelete = dbContext.Events.Find(Id);
            eventToDelete.IsDeleted = true;
            dbContext.SaveChanges();
        }

     
        public async Task<List<Event>> GetUserEvents(string userId)
        {
            return await dbContext.Events.Where(x => x.CreatedBy == userId && !x.IsDeleted).ToListAsync();
        }

        public async Task<bool> ExitQueue(string userId, long lineMemberId, string adminId = "")
        {
            Line line = dbContext.Lines
                .Where(x => x.LineMemberId == lineMemberId)
                .Include(x=>x.LineMember)
                .FirstOrDefault();

            await lineRepo.MarkUserAsAttendedTo(line, adminId!= "" ? "left" : "served by Admin" );
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
            var allEvents = dbContext.Events.AsQueryable();

            int pageCount = (await allEvents.CountAsync() + size - 1) / size;

            var filteredEvents = string.IsNullOrEmpty(query)
                ? allEvents
                : allEvents.Where(x => x.Title.Contains(query));

            var eventsData = await filteredEvents
                .OrderBy(x => x.EventStartTime)
                .Skip((page - 1) * size)
                .Take(size)
                .Include(x => x.SwiftLineUser)
                .Where(x=>!x.IsDeleted)
                .ToListAsync();

            var events = eventsData.Select(x => new Event
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                AverageTime = x.AverageTime,
                EventStartTime = x.EventStartTime,
                EventEndTime = x.EventEndTime,
                UsersInQueue = x.UsersInQueue,
                Organizer = x.SwiftLineUser.UserName,
                HasStarted = isEventActiveRightNow(x),
                StaffCount = x.StaffCount,
                IsActive = x.IsActive,
                AllowAnonymousJoining = x.AllowAnonymousJoining,
            }).ToList();
            var user= await getUser(userId);
            return new SearchEventsRes(events, pageCount, user is null ? false : user.IsInQueue,
                user is null ? 0 : user.LastEventJoined);
        }

        private async Task<SwiftLineUser> getUser(string userId)
        {
            return await dbContext.SwiftLineUsers.FindAsync(userId);
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
