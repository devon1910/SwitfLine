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

            // Create a single query that gets both active events by time and events with unfinished lines
            var activeEvents = await dbContext.Events
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted && (
                    // Time-based active events
                    (
                        // For events that do not span midnight
                        (x.EventStartTime <= x.EventEndTime &&
                         timeNow >= x.EventStartTime &&
                         timeNow <= x.EventEndTime)
                        ||
                        // For events that span midnight
                        (x.EventStartTime > x.EventEndTime &&
                         (timeNow >= x.EventStartTime || timeNow <= x.EventEndTime))
                    )
                    ||
                    // Events with unfinished queue items
                    dbContext.Lines
                        .Any(l => !l.IsAttendedTo &&
                              l.LineMember.EventId == x.Id &&
                              !l.LineMember.Event.IsDeleted)
                ))
                .ToListAsync();

            return activeEvents;
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
            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                // Fetch event and validate in one query with proper includes
                var eventEntity = await dbContext.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == eventId && !e.IsDeleted);

                if (eventEntity == null)
                {
                    return AuthResFailed.CreateFailed("Event not found");
                }

                if (!IsEventActive(eventEntity.EventStartTime, eventEntity.EventEndTime))
                {
                    return AuthResFailed.CreateFailed("Event hasn't started.");
                }

                // Get or create user
                SwiftLineUser user = await getUser(userId);
                string token = string.Empty;
                bool isNewUser = false;

                if (user is null)
                {
                    if (!eventEntity.AllowAnonymousJoining)
                    {
                        return AuthResFailed.CreateFailed(
                            "The Event Organizer has disabled anonymous joining. Please login or sign up to join this queue");
                    }

                    var creationResult = await authRepo.CreateAnonymousUser();
                    if (!creationResult.status)
                    {
                        return AuthResFailed.CreateFailed(
                            "Unable to create an anonymous account. Please try again later");
                    }

                    user = creationResult.user;
                    token = creationResult.AccessToken;
                    userId = user.Id;
                    isNewUser = true;
                }
               

                if (user.IsInQueue)
                {
                    return AuthResFailed.CreateFailed("You are already in a queue");
                }

                // Create queue entry in a single operation
                var newQueueMember = new LineMember
                {
                    EventId = eventId,
                    UserId = userId
                };

                dbContext.LineMembers.Add(newQueueMember);
                await dbContext.SaveChangesAsync();

                dbContext.Lines.Add(new Line
                {
                    LineMemberId = newQueueMember.Id
                });

                // Update user info
                user.IsInQueue = true;
                user.LastEventJoined = eventId;

                // Increment users in queue atomically
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE public.\"Events\" SET \"UsersInQueue\" = \"UsersInQueue\" + 1 WHERE \"Id\" = {eventId}");

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AuthRes(
                    true,
                    "Joined queue successfully",
                    token,
                    "",
                    user.Id,
                    user.Email,
                    user.UserName,
                    "",
                    isNewUser
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log exception details
                return AuthResFailed.CreateFailed("An error occurred while joining the queue");
            }

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

        public async Task<bool> ExitQueue(string userId, long lineMemberId, string adminId = "", int position = -1)
        {
            Line line = dbContext.Lines
                .Where(x => x.LineMemberId == lineMemberId)
                .Include(x=>x.LineMember)
                .FirstOrDefault();

            await lineRepo.MarkUserAsServed(line, adminId!= "" ? "left" : "served by Admin" );
            await notifier.BroadcastLineUpdate(line,position);
            await lineRepo.Notify2ndLineMember(line);
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
            var baseQuery = dbContext.Events
       .AsNoTracking()
       .Where(x => !x.IsDeleted);

            // Only apply filter if query exists
            if (!string.IsNullOrEmpty(query))
            {
                baseQuery = baseQuery.Where(x => EF.Functions.Like(x.Title, $"%{query}%"));
            }

            baseQuery = baseQuery.OrderBy(x => x.EventStartTime);

            // Execute one query to get total count
            var totalCount = await baseQuery.CountAsync();
            var pageCount = (totalCount + size - 1) / size;

            // Execute second query to get paginated data
            var eventsData = await baseQuery
                .Skip((page - 1) * size)
                .Take(size)
                .Include(x => x.SwiftLineUser)
                .ToListAsync();

            // Process the data in memory since the time comparison logic can't be translated to SQL
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
                HasStarted = IsEventActive(x.EventStartTime, x.EventEndTime),
                StaffCount = x.StaffCount,
                IsActive = x.IsActive,
                AllowAnonymousJoining = x.AllowAnonymousJoining,
            }).ToList();

            // Execute user query in parallel
            var user = await getUser(userId);

            return new SearchEventsRes(
                events,
                pageCount,
                user?.IsInQueue ?? false,
                user?.LastEventJoined ?? 0
            );
        }

        private async Task<SwiftLineUser> getUser(string userId)
        {
            return await dbContext.SwiftLineUsers.FindAsync(userId);
        }

        private static bool IsEventActive(TimeOnly startTime, TimeOnly endTime)
        {
            var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));

            if (startTime <= endTime)
            {
                return currentTime >= startTime && currentTime <= endTime;
            }
            else
            {
                return currentTime >= startTime || currentTime <= endTime;
            }
        }





    }
}
