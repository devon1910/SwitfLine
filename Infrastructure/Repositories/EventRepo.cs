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


        public async Task<bool> CreateEvent(string userId, CreateEventModel req)
        {
            try
            {
                if (await EventExists(req.Title))
                {
                    return false;
                }

                if (req.EnableGeographicRestriction && (req.Latitude == null || req.Longitude == null || req.RadiusInMeters == default))
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
                    AllowAutomaticSkips = req.AllowAutomaticSkips,
                    EnableGeographicRestriction = req.EnableGeographicRestriction,
                    Address = req.Address,
                    Longitude = req.Longitude,
                    Latitude = req.Latitude,
                    RadiusInMeters = req.RadiusInMeters
                };

                await dbContext.Events.AddAsync(newEvent);
                await dbContext.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
          
            
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
            @event.AllowAutomaticSkips = req.AllowAutomaticSkips;
            @event.EnableGeographicRestriction = req.EnableGeographicRestriction;
            @event.Address = req.Address;
            @event.Longitude = req.Longitude;
            @event.Latitude = req.Latitude;
            @event.RadiusInMeters = req.RadiusInMeters;
            await dbContext.SaveChangesAsync();
            return true;

        }

        public async Task<List<Event>> GetActiveEvents()
        {
            var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));

            // Create a single query that gets both active events by time and events with unfinished lines
            var activeEvents = await dbContext.Events
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted && x.AllowAutomaticSkips && (
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

        public async Task<EventQueueRes> GetEventQueue(int currentMembersPage, int pastMembersPage, int size, long eventId)
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
                   .Skip((currentMembersPage - 1) * size)
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
                  .Skip((pastMembersPage - 1) * size)
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

                linesMembersInQueue = [.. linesMembersInQueue.OrderBy(x => x.CreatedAt)];

                pastLineMembers = [.. pastLineMembers.OrderByDescending(x => x.CreatedAt)];

                int TotalServed = 0;
                int dropOffRate = 0;
                int averageTime = 0;
                List<AttendanceData> attendanceData = null;
                List<DropOffRateTrend> dropOffRateTrend = null;
                List<DropOffReason> dropOffReasons =null;
                List<PeakArrivalPeriodData> peakArrivalPeriodData = null;


                if (currentMembersPage == 1 && pastMembersPage == 1) 
                {
                    TotalServed = allLines.Where(x => x.EventId == eventId && x.Status.Contains("served")).Count();

                    int peopleThatHaveLeft = allLines
                       .Where(x => x.EventId == eventId && x.Status.Contains("left"))
                       .Count();

                    double test = Math.Round(((double)peopleThatHaveLeft / allPastMembersInQueue) * 100, 2);
                    dropOffRate = (int)Math.Ceiling(test);



                    if (allLines.Any())
                    {
                        averageTime = (int)Math.Ceiling(allLines.Select(x => x.TimeWaited).Average());
                    }

                    attendanceData = allLines
                    .Where(x => (x.Status == "served" || x.Status == "left"))
                    .GroupBy(x => x.CreatedAt.Month)
                    .Select(g => new AttendanceData
                    {
                        Month = g.Key,
                        ServedCount = g.Count(x => x.Status == "served"),
                        AttendeesCount = g.Count(x => x.Status == "left" || x.Status == "served"),

                    })
                    .OrderBy(g => g.Month)
                    .ToList();


                    dropOffRateTrend = attendanceData
                     .Select(x => new DropOffRateTrend
                     {
                         Month = x.Month,
                         DropOffRate = (int)Math.Ceiling((double)(x.AttendeesCount - x.ServedCount) / x.AttendeesCount * 100)
                     })
                     .ToList();

                    dropOffReasons = [.. allLines
                    .Where(x => x.IsAttendedTo && !string.IsNullOrEmpty(x.LeaveQueueReason))
                    .GroupBy(x => x.LeaveQueueReason)
                    .Select(g => new DropOffReason
                    {
                        Reason = g.Key,
                        Count = g.Count()
                    })];

                    peakArrivalPeriodData = allLines
                        .Where(x => x.IsAttendedTo)
                        .GroupBy(x => x.TimeOfDay)
                        .Select(g => new PeakArrivalPeriodData
                        {
                            TimeOfDay = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(g => g.Count)
                        .ToList();

                }

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

     
        public async Task<GetUserEventsRes> GetUserEvents(string userId)
        {
            try
            {
                var events = await dbContext.Events.Where(x => x.CreatedBy == userId && !x.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
                List<long> allEventsIds = events.Select(x => x.Id).ToList();

                var allLinesData = dbContext.Lines.Where(x => allEventsIds
                .Contains(x.EventId))
                    .Include(x => x.Event)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .ToList();

                List<ComparisonMetric> totalAttendees = allLinesData
                    .GroupBy(x => x.EventId)
                    .Select(g => new ComparisonMetric
                    {
                        EventId = g.Key,
                        Count = g.Count(),
                        EventName = g.FirstOrDefault().Event.Title
                    }).ToList();

                List<ComparisonMetric> totalServed = allLinesData
                    .Where(x => x.IsAttendedTo && x.Status.Contains("served"))
                    .GroupBy(x => x.EventId)
                    .Select(g => new ComparisonMetric
                    {
                        EventId = g.Key,
                        Count = g.Count(),
                        EventName = g.FirstOrDefault().Event.Title
                    }).ToList();

                List<ComparisonMetric> dropOffRate = allLinesData
                    .Where(x => x.IsAttendedTo && x.Status.Contains("left"))
                    .GroupBy(x => x.EventId)
                    .Select(g => new ComparisonMetric
                    {
                        EventId = g.Key,
                        Count = (int)Math.Ceiling(((double)g.Count() / totalAttendees.FirstOrDefault(x=>x.EventId == g.Key).Count) * 100),
                        EventName = g.FirstOrDefault().Event.Title
                    }).ToList();

                EventComparisonData eventComparisonData = new(totalAttendees, totalServed, dropOffRate);

                return new GetUserEventsRes(events, eventComparisonData);

            }
            catch (Exception ex)
            {

                throw;
            }
           
        }

        public async Task<bool> ExitQueue(string userId, long lineMemberId, string adminId = "", int position = -1, string leaveQueueReason = "")
        {
            Line? line = dbContext.Lines
                .Where(x => (x.UserId == userId || x.Id==lineMemberId) && !x.IsAttendedTo)
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
                .OrderByDescending(x=>x.CreatedAt)
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
                EnableGeographicRestriction = x.EnableGeographicRestriction,
                RadiusInMeters = x.RadiusInMeters,
                Address = x.Address
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
