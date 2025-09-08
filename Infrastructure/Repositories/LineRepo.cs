using Application.Services;
using Azure.Core;
using Domain;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql.Replication.TestDecoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Infrastructure.Repositories
{
    public class LineRepo(SwiftLineDatabaseContext dbContext, IConfiguration _configuration, IEmailsDeliveryRepo emailRepo) : ILineRepo
    {
     
        public async Task<bool> IsItUserTurnToBeServed(Line line, int EventAverageWaitSeconds)
        {
            if (line.DateStartedBeingAttendedTo == default) //first on the queue
            {
                line.DateStartedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
                await dbContext.SaveChangesAsync();
            }

            var diff = (DateTime.UtcNow.AddHours(1) - line.DateStartedBeingAttendedTo).TotalSeconds;

            return diff >= EventAverageWaitSeconds;


        }

        public async Task<bool> MarkUserAsServed(Line line, string status, string leaveQueueReason)
        {
           
            try
            {
                line.IsAttendedTo = true;
                line.DateCompletedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
                line.Status = leaveQueueReason == "Got served" ? "served" : status;
                line.LeaveQueueReason = leaveQueueReason;
                DateTime completedDate = line.DateStartedBeingAttendedTo != default ? line.DateStartedBeingAttendedTo : DateTime.UtcNow.AddHours(1);
                line.TimeWaited = Math.Round((completedDate- line.DateCompletedBeingAttendedTo).TotalMinutes,2);
 

                await dbContext.Database.ExecuteSqlInterpolatedAsync(
     $"UPDATE public.\"Events\" set \"UsersInQueue\"=\"UsersInQueue\" - 1 where \"Id\"={line.EventId} AND \"UsersInQueue\" > 0");
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
   $"UPDATE public.\"AspNetUsers\" set \"IsInQueue\"='false' where \"Id\"={line.UserId}");
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
   $"UPDATE public.\"AspNetUsers\" set \"LastEventJoined\"=0 where \"Id\"={line.UserId}");


                await dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
          
        }

        public async Task<List<Line?>> GetFirstLineMembers(long eventId, int numberOfStaffServing)
        {
            return await  dbContext.Lines
                .Where(x => x.IsActive && !x.IsAttendedTo && x.EventId == eventId)
                .OrderBy(x => x.Id)
                .Skip(0).Take(numberOfStaffServing)
                .ToListAsync();      
        }


        public async Task<LineInfoRes> GetUserLineInfo(string UserId)
        {

            try
            {
                var line = await dbContext.Lines
                .Where(x => !x.IsAttendedTo && x.IsActive && x.UserId == UserId)
                .FirstOrDefaultAsync();

                if (line is null)
                    return new LineInfoRes(-1, 0, "", "", 0);

                // Get the full queue
                var othersInLines = await dbContext.Lines
                    .Where(x => x.IsActive && !x.IsAttendedTo && x.EventId == line.EventId)
                    .OrderBy(x => x.Id)
                    .AsNoTracking()
                    .ToListAsync();

                // Get event info
                var @event = await dbContext.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == line.EventId);

                // Find position (correctly by matching ID)
                int index = othersInLines.FindIndex(x => x.Id == line.Id);
                int actualPosition = index + 1;

                // Estimate wait time
                int timeTillYourTurn = 0;
                double totalMinutes = (actualPosition * @event.AverageTime) / (double)@event.StaffCount;
                timeTillYourTurn = (int)Math.Ceiling(totalMinutes);
                timeTillYourTurn = Math.Max(timeTillYourTurn, @event.AverageTime);
              

                int position = (int)Math.Ceiling((decimal)timeTillYourTurn / @event.AverageTime);               

                int timeTillYourTurnAI = 0;

                if (@event.AverageTime > 5)
                {
                    timeTillYourTurnAI = timeTillYourTurn - @event.AverageTime;
                }
                else 
                {
                    int eqp = Math.Max(actualPosition - @event.StaffCount, 0);
                    int batches = eqp / @event.StaffCount; // tell the model how to handle parallelism
                                                           //using ML for estimating wait time
                    WaitTimeEstimator.ModelInput sampleData = new WaitTimeEstimator.ModelInput()
                    {
                        AvgServiceTimeWhenJoined = @event.AverageTime,
                        NumActiveServersWhenJoined = @event.StaffCount,
                        EffectiveQueuePosition = eqp, //Number of people ahead of me
                        Batches = batches
                    };

                    var predictionResult = WaitTimeEstimator.Predict(sampleData);
                    timeTillYourTurnAI = @event.StaffCount > 1 ? (int)Math.Ceiling(predictionResult.Score) : (int)Math.Floor(predictionResult.Score);
                }
                    
                var wordChainGameLeaderboardRecord = dbContext.WordChainGameLeaderboard.Where(x => x.UserId == UserId).FirstOrDefault();

                int HighestScore = 0;

                if (wordChainGameLeaderboardRecord is not null) {

                    HighestScore = wordChainGameLeaderboardRecord.HighestScore;
                }
              
                // Return final result
                return new LineInfoRes(
                    position,
                    timeTillYourTurn - @event.AverageTime,
                    GetOrdinal(position),
                    @event.Title,
                    @event.AverageTime,
                    @event.IsActive,
                    @event.StaffCount,
                    timeTillYourTurnAI,
                    @event.AllowAutomaticSkips,
                    HighestScore
                );

            }
            catch (Exception ex)
            {

                throw;
            }
            

        }


        public async Task Notify2ndLineMember(Line line)
        {
            try
            {
                long eventId = line.EventId;

                // Combine queries to reduce database round trips
                var result = await dbContext.Events
                    .Where(e => e.Id == eventId)
                    .Select(e => new
                    {
                        Event = e,
                        FifthUser = dbContext.Lines
                            .Where(l => l.IsActive &&
                                   !l.IsAttendedTo &&
                                   l.EventId == eventId)
                            .Include(x=>x.SwiftLineUser)
                            .AsNoTracking()
                            .AsSplitQuery()
                            .OrderBy(l => l.Id)
                            .Skip(1) 
                            .Select(l => new
                            {
                                Email = l.SwiftLineUser.Email,
                                Username = l.SwiftLineUser.UserName
                            })
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                // Early exit if event doesn't exist or no eligible user found
                if (result?.Event == null || result.FifthUser == null)
                    return;

                // Calculate estimated time once
                int estimatedTime = result.Event.AverageTimeToServeSeconds / 60;

               
                if (!result.FifthUser.Username.StartsWith("Anonymous")) 
                {
                    await emailRepo.LogEmail(
                   email: result.FifthUser.Email,
                   subject: $"Your Turn is Coming Up Soon - theSwiftLine",
                   link: _configuration["SwiftLineBaseUrl"] + "myQueue",
                   type: EmailTypeEnum.Reminder,
                   username: result.FifthUser.Username,
                   estimatedWait: estimatedTime.ToString()
                   );
                }
            }
            catch (Exception ex)
            {
                // Log the exception (if logging is implemented)
                throw;
            }
        }

        private static string GetOrdinal(int number)
        {
            int lastTwo = number % 100;
            if (lastTwo >= 11 && lastTwo <= 13) return "th";

            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th",
            };
        }

        public async Task<List<WordChainGameLeaderboard>> GetTop10Players()
        {
             
             return await dbContext.WordChainGameLeaderboard
                .OrderByDescending(x=>x.HighestScore)
                .Skip(0)
                .Take(10)
                .Include(x=>x.SwiftLineUser)
                .Select(x=> new WordChainGameLeaderboard {
                    UserId=x.UserId, HighestScore=x.HighestScore, 
                    Level=x.Level,
                    Rank= dbContext.WordChainGameLeaderboard.Count(y=>y.HighestScore>x.HighestScore)+1,//on the fly ranking
                    Username = x.SwiftLineUser.UserName.StartsWith("Anonymous") ? "Anonymous" : x.SwiftLineUser.UserName
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateUserScore(string UserId, LeaderboardUpdateReq req)
        {
            var isUpdated = dbContext.WordChainGameLeaderboard
                .Where(x => x.UserId == UserId)
                .ExecuteUpdate(x => x.SetProperty(p => p.HighestScore, p => req.score));

            if (isUpdated == 0 && !string.IsNullOrEmpty(UserId)) 
            {
                var newRecord = new WordChainGameLeaderboard
                {
                    UserId = UserId,
                    HighestScore = req.score,
                    Level = req.level
                    
                };
                await dbContext.WordChainGameLeaderboard.AddAsync(newRecord);
                await dbContext.SaveChangesAsync();               
            }
            return true;
        }
    }
}
