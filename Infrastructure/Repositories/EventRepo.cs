using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Infrastructure.Repositories
{

    public class EventRepo(SwiftLineDatabaseContext dbContext, ILineRepo lineRepo, ISignalRNotifierRepo notifier, IAuthRepo authRepo) : IEventRepo 
    {
      
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
                              l.EventId == x.Id &&
                              !l.Event.IsDeleted)
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

            try
            {
                var allLines = dbContext.Lines
               .Where(x => x.EventId == eventId)
               .AsNoTracking();


                var allIndividualsInQueue = allLines.Where(x => !x.IsAttendedTo).Count();

                var allPastMembersInQueue = allLines.Where(x => x.IsAttendedTo).Count();

                int pageCountInQueue = (int)Math.Ceiling(allIndividualsInQueue / (double)size);

                int pageCountPastMembers = (int)Math.Ceiling(allPastMembersInQueue / (double)size);

                var linesMembersInQueue = await allLines
                   .Where(x => !x.IsAttendedTo)
                   .Skip((page - 1) * size)
                   .Take(size)
                   .Select(x => new Line
                   {
                       Id = x.Id,
                       CreatedAt = x.CreatedAt.AddHours(-1),
                       DateCompletedBeingAttendedTo = x.DateCompletedBeingAttendedTo.AddHours(-1),
                       DateStartedBeingAttendedTo = x.DateStartedBeingAttendedTo.AddHours(-1),
                       IsAttendedTo = x.IsAttendedTo,
                       Status = x.Status,
                       TimeWaited = x.TimeWaited,
                       SwiftLineUser = new SwiftLineUser
                       {
                           UserName = x.SwiftLineUser.UserName,
                       },
                   }).ToListAsync();

                var pastLineMembers = await allLines
                  .Where(x => x.IsAttendedTo)
                  .Skip((page - 1) * size)
                  .Take(size)
                  .Select(x => new Line
                  {
                      Id = x.Id,
                      CreatedAt = x.CreatedAt.AddHours(-1),
                      DateCompletedBeingAttendedTo = x.DateCompletedBeingAttendedTo.AddHours(-1),
                      DateStartedBeingAttendedTo = x.DateStartedBeingAttendedTo.AddHours(-1),
                      IsAttendedTo = x.IsAttendedTo,
                      Status = x.Status,
                      TimeWaited = x.TimeWaited,
                      SwiftLineUser = new SwiftLineUser
                      {
                          UserName = x.SwiftLineUser.UserName,
                      },
                  }).ToListAsync();

                var isEventActive = dbContext.Events.Find(eventId).IsActive;

                var TotalServed = allLines.Where(x => x.EventId == eventId && x.Status.Contains("served")).Count();

                int peopleThatHaveLeft = allLines
                   .Where(x => x.EventId == eventId && x.Status.Contains("left"))
                   .Count();

                double test = Math.Round(((double)peopleThatHaveLeft / allPastMembersInQueue) * 100,2);
                int dropOffRate = (int) Math.Ceiling( test);

                int averageTime = 0;

                if (allLines.Any())
                {
                    averageTime = (int)Math.Ceiling(allLines.Select(x => x.TimeWaited).Average());
                }

                linesMembersInQueue = [.. linesMembersInQueue.OrderBy(x => x.CreatedAt)];

                pastLineMembers = [.. pastLineMembers.OrderByDescending(x => x.CreatedAt)];

                var attendanceData = allLines
                 .Where(x => (x.Status == "served" || x.Status == "left"))
                 .GroupBy(x => x.CreatedAt.Month)
                 .Select(g => new
                 {
                     Month =g.Key,
                     ServedCount = g.Count(x => x.Status == "served"),
                     attendeesCount = g.Count(x => x.Status == "left" || x.Status=="served"),

                 })
                 .OrderBy(g => g.Month)
                 .ToList();


                var dropOffRateTrend = attendanceData
                 .Select(x => new
                 {
                     Month = x.Month,
                     DropOffRate = (int)Math.Ceiling((double)(x.attendeesCount - x.ServedCount) / x.attendeesCount * 100)
                 })
                 .ToList();

                var dropOffReasons = allLines
                    .Where(x => x.IsAttendedTo && !string.IsNullOrEmpty(x.LeaveQueueReason))
                    .GroupBy(x => x.LeaveQueueReason)
                    .Select(g => new
                    {
                        Reason = g.Key,
                        Count = g.Count()
                    })
                    .ToList();

                var peakArrivalPeriodData = allLines
                    .Where(x=> x.IsAttendedTo)
                    .GroupBy(x => x.TimeOfDay)
                    .Select(g => new
                    {
                        TimeOfDay =  g.Key,                     
                        Count = g.Count()
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();


                return new EventQueueRes(
                    linesMembersInQueue, pastLineMembers,
                    !isEventActive, pageCountInQueue,
                    pageCountPastMembers,
                    TotalServed, averageTime,
                    dropOffRate, attendanceData, dropOffRateTrend,
                    dropOffReasons, peakArrivalPeriodData);

            }
            catch (Exception ex)
            {

                throw;
            }
           
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
                    return AuthResFailed.CreateFailed("You are already in a queue.");
                }

                int totalLineMembersInQueue = eventEntity.UsersInQueue + 1;
                int PositionInQueue = (int) Math.Ceiling((double)(totalLineMembersInQueue) / eventEntity.StaffCount);

                dbContext.Lines.Add(new Line
                {
                    EventId = eventId,
                    UserId = userId,
                    AvgServiceTimeWhenJoined = eventEntity.AverageTime,
                    NumActiveServersWhenJoined = eventEntity.StaffCount,
                    TimeWaited = 0,
                    TimeOfDay = getTimeOfTheDay(DateTime.UtcNow.AddHours(1).Hour),
                    DayOfWeek = (DayOfWeekEnum) DateTime.UtcNow.DayOfWeek,
                    EffectiveQueuePosition =  Math.Max(0, (PositionInQueue - eventEntity.StaffCount)), 
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
                return AuthResFailed.CreateFailed($"An error occurred while joining the queue");
            }

        }

        private TimeOfDayEnum getTimeOfTheDay(int hour) 
        {
            TimeOfDayEnum timeOfDayEnum;

            if (hour >= 6 && hour < 12)
                timeOfDayEnum = TimeOfDayEnum.Morning;
            else if (hour >= 12 && hour < 18)
                timeOfDayEnum = TimeOfDayEnum.Afternoon;
            else if (hour >= 18 && hour < 22)
                timeOfDayEnum = TimeOfDayEnum.Evening;
            else
                timeOfDayEnum = TimeOfDayEnum.Night;

            return  timeOfDayEnum;
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

        public async Task<bool> ExitQueue(string userId, long lineMemberId, string adminId = "", int position = -1, string leaveQueueReason = "")
        {
            Line? line = dbContext.Lines
                .Where(x => x.UserId == userId && !x.IsAttendedTo)
                .FirstOrDefault();

            if (line is null) return false;

            await lineRepo.MarkUserAsServed(line, adminId!= "" ? "left" : "served by Admin", leaveQueueReason);
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

        public async Task<bool> EventExists(string title)
        {
            Event existingEvent = await dbContext.Events.FirstOrDefaultAsync(x => x.Title == title);

            return existingEvent is null ? false : true;
        }

        public async Task AddEvent(Event newEvent)
        {
            await dbContext.Events.AddAsync(newEvent);
        }

        public async Task<int> SaveChangesAsync()
        {
           return await dbContext.SaveChangesAsync();
        }
    }
}
