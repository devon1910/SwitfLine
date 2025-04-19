using Application.Services;
using Azure.Core;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Infrastructure.Repositories
{
    public class LineRepo(SwiftLineDatabaseContext dbContext, IConfiguration _configuration, IFluentEmail _fluentEmail) : ILineRepo
    {
       
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

        public async Task<List<Line>> GetLines()
        {
            return await dbContext.Lines
                .Where(x=>x.IsActive && !x.IsAttendedTo)
                .Include(x => x.LineMember)
                .ThenInclude(x => x.Event)
                .AsSplitQuery()
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
            
        }

        public async Task<bool> IsUserAttendedTo(Line line)
        {
            if (line.DateStartedBeingAttendedTo == default) //first on the queue
            {
                line.DateStartedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
                await dbContext.SaveChangesAsync();
            }

            var diff = (DateTime.UtcNow.AddHours(1) - line.DateStartedBeingAttendedTo).TotalSeconds;

            if (diff >= line.LineMember.Event.AverageTimeToServeSeconds) return true;
            return false;
        }

        public async Task<bool> MarkUserAsAttendedTo(Line line, string status)
        {
            //line.IsAttendedTo = true;
            //line.DateCompletedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
            //line.Status = status;
            //SwiftLineUser? user = await dbContext.SwiftLineUsers.FindAsync(line.LineMember.UserId);
            //user.IsInQueue = false;
            //await dbContext.SaveChangesAsync();
            //return true;

            // Explicitly load the LineMember with its SwiftLineUser

            try
            {
                line.IsAttendedTo = true;
                line.DateCompletedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
                line.Status = status;

                await dbContext.Database.ExecuteSqlInterpolatedAsync(
   $"UPDATE public.\"Events\" set \"UsersInQueue\"=\"UsersInQueue\" - 1 where \"Id\"={line.LineMember.EventId}");
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
   $"UPDATE public.\"AspNetUsers\" set \"IsInQueue\"='false' where \"Id\"={line.LineMember.UserId}");
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
   $"UPDATE public.\"AspNetUsers\" set \"LastEventJoined\"=0 where \"Id\"={line.LineMember.UserId}");


                await dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
           

            //using var transaction = await dbContext.Database.BeginTransactionAsync();
            //try
            //{
            //    await dbContext.SaveChangesAsync();
            //    await transaction.CommitAsync();
            //    return true;
            //}
            //catch (DbUpdateConcurrencyException ex)
            //{
            //    await transaction.RollbackAsync();
            //    // Implement concurrency handling
            //    foreach (var entry in ex.Entries)
            //    {
            //        var databaseValues = await entry.GetDatabaseValuesAsync();
            //        if (databaseValues != null)
            //        {
            //            entry.OriginalValues.SetValues(databaseValues);
            //            entry.CurrentValues.SetValues(databaseValues);
            //            line.LineMember.SwiftLineUser.IsInQueue = false; // Re-apply
            //        }
            //    }
            //    await dbContext.SaveChangesAsync();
            //    await transaction.CommitAsync();
            //    return true;
            //}
            //catch
            //{
            //    await transaction.RollbackAsync();
            //    throw;
            //}

        }

        public async Task<Line?> GetFirstLineMember(long eventId)
        {
            return await  dbContext.Lines
                .Where(x => x.IsActive && !x.IsAttendedTo)
                .Include(x => x.LineMember)
                .ThenInclude(x => x.Event)
                .AsSplitQuery()
                .Where(x => x.LineMember.EventId == eventId)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();      
        }


        public async Task<LineInfoRes> GetUserLineInfo(string UserId)
        {

                var line = await  dbContext.Lines
                
                .Where(x => !x.IsAttendedTo && x.IsActive)
                .Include(x => x.LineMember)
                .AsSplitQuery()
                .Where(x => x.LineMember.UserId == UserId)
                .FirstOrDefaultAsync();

                if(line is null) return new LineInfoRes(0, -1, 0, "", "");

                int position = 0;
                var othersInLines = await dbContext.Lines
                       .Where(x => x.IsActive && !x.IsAttendedTo)
                       .Include(x => x.LineMember)
                       .AsSplitQuery()
                       .Where(x => x.LineMember.EventId == line.LineMember.EventId)
                       .OrderBy(x => x.CreatedAt)
                       .AsNoTracking()
                       .ToListAsync();

               

                Event @event = dbContext.Events.AsNoTracking().FirstOrDefault(x=>x.Id==line.LineMember.EventId);

                //int timeTillYourTurn = ((@event.AverageTimeToServeSeconds * position) - @event.AverageTimeToServeSeconds) / 60;
                double totalMinutes = (othersInLines.Count * @event.AverageTime) / (double)@event.StaffCount;

                // Round up to nearest minute
                int timeTillYourTurn = (int)Math.Ceiling(totalMinutes);

                // Ensure minimum wait time is at least the average service time
                timeTillYourTurn = Math.Max(timeTillYourTurn, @event.AverageTime);

                //HEREEEEE

                position = (int) Math.Ceiling((decimal)timeTillYourTurn/ @event.AverageTime);

                return new LineInfoRes(line.LineMemberId, position, timeTillYourTurn,
                    GetOrdinal(position), @event.Title, @event.IsActive, @event.StaffCount);  
        }

        public bool GetUserQueueStatus(string UserId)
        {
            var user=  dbContext.SwiftLineUsers.Find( UserId);
            return user is not null ? user.IsInQueue : false;
            
        }

        public async Task NotifyFifthMember(Line line)
        {
            long eventId = line.LineMember.EventId;
            Event @event = dbContext.Events.Find(line.LineMember.EventId);
            var user = await dbContext.Lines
                .Where(x => x.IsActive && !x.IsAttendedTo)
                .Include(x => x.LineMember.SwiftLineUser)
                .AsSplitQuery()
                .Where(x => x.LineMember.EventId == eventId)
                .OrderBy(x => x.CreatedAt)
                .Skip(1)
                .FirstOrDefaultAsync();
            
           
            if (user is not null) 
            {
                int EstimatedTime = (@event.AverageTimeToServeSeconds) / 60; //nptifies the 2nd person for now
                try
                {
                    await SendReminderMail(user.LineMember.SwiftLineUser.Email, EstimatedTime, user.LineMember.SwiftLineUser.UserName);
                }
                catch (Exception ex)
                {

                    throw;
                }
                
            }

        }
        private async Task<bool> SendReminderMail(string RecipientEmail, int EstimatedTime, string username)
        {
            string htmlTemplate = GetEmailTemplate();
            string link = _configuration["SwiftLineBaseUrlForReminder"]; 
            var email = await _fluentEmail
                .To(RecipientEmail)
                .Subject($"Your Turn is Coming Up Soon - Swiftline ⏭")
                .Body(htmlTemplate
                .Replace("[UserName]", username)
                .Replace("[swiftlinelink]", link)
                .Replace("[estimatedTime]",EstimatedTime.ToString()), true)
                .SendAsync();
            if (!email.Successful)
            {
                string.Join(", ", email.ErrorMessages);
                throw new Exception("Failed to send welcome Email");
            }
            return true;
        }
       

    
        private string GetEmailTemplate()
        {
            return @"<!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Your Turn is Coming Up Soon - Swiftline</title>
                            <style>
                                /* Base styles */
                                body {
                                    margin: 0;
                                    padding: 0;
                                    font-family: 'Helvetica Neue', Arial, sans-serif;
                                    line-height: 1.6;
                                    color: #333333;
                                    background-color: #f5f5f5;
                                }
        
                                .container {
                                    max-width: 600px;
                                    margin: 0 auto;
                                    background-color: #ffffff;
                                }
        
                                /* Header styles */
                                .header {
                                    background-color: #7D9D74; /* Sage green */
                                    padding: 24px;
                                    text-align: center;
                                }
        
                                .logo {
                                    max-width: 180px;
                                }
        
                                /* Content styles */
                                .content {
                                    padding: 30px;
                                }
        
                                h1 {
                                    color: #333333; /* Black */
                                    font-size: 24px;
                                    margin-top: 0;
                                    margin-bottom: 20px;
                                }
        
                                p {
                                    margin-bottom: 20px;
                                }
        
                                /* Timer styles */
                                .timer-container {
                                    text-align: center;
                                    margin: 25px 0;
                                    padding: 20px;
                                    background-color: #F5F8F4; /* Light sage */
                                    border-radius: 8px;
                                }
        
                                .estimated-time {
                                    font-size: 36px;
                                    font-weight: bold;
                                    color: #7D9D74; /* Sage green */
                                    margin: 10px 0;
                                }
        
                                .time-label {
                                    font-size: 16px;
                                    color: #666666;
                                }
        
                                /* Button styles */
                                .button-container {
                                    text-align: center;
                                    margin: 30px 0;
                                }
        
                                .button {
                                    display: inline-block;
                                    background-color: #7D9D74; /* Sage green */
                                    color: #ffffff; /* White */
                                    text-decoration: none;
                                    padding: 14px 30px;
                                    border-radius: 4px;
                                    font-weight: bold;
                                    letter-spacing: 0.5px;
                                    text-transform: uppercase;
                                    font-size: 16px;
                                }
        
                                /* Urgency note */
                                .urgency-note {
                                    border-left: 4px solid #7D9D74; /* Sage green */
                                    padding: 15px;
                                    margin-bottom: 25px;
                                    background-color: #F5F8F4; /* Light sage */
                                }
        
                                /* Footer styles */
                                .footer {
                                    background-color: #333333; /* Black */
                                    color: #ffffff;
                                    padding: 20px;
                                    text-align: center;
                                    font-size: 12px;
                                }
        
                                .footer-links a {
                                    color: #ffffff;
                                    text-decoration: none;
                                    margin: 0 10px;
                                }
                                 .center {
                                    display: block;
                                    margin-left: auto;
                                    margin-right: auto;
                                    width: 30%;
                                }
                            </style>
                        </head>
                        <body>
                            <div class=""container"">

                                <div>
                                    <img src = ""https://res.cloudinary.com/dddabj5ub/image/upload/v1741908218/swifline_logo_cpsacv.webp"" alt=""Swiftline"" class=""center"">
                                </div>
                                
                                <div class=""content"">
                                    <h1>You're Almost Up!</h1>
            
                                    <p>Hello [UserName],</p>
            
                                    <p>Great news! Your turn in the queue is coming up very soon. Please make sure you're ready and stay nearby.</p>
            
                                    <div class=""timer-container"">
                                        <div class=""time-label"">Estimated time until your turn:</div>
                                        <div class=""estimated-time"">[estimatedTime] minutes</div>
                                    </div>
            
                                    <div class=""urgency-note"">
                                        <strong>Important:</strong> To maintain your place in line, please be ready when it's your turn. If you miss your slot, you may need to rejoin the queue.
                                    </div>
            
                                    <p>Check your current status and receive live updates by returning to the app.</p>
            
                                    <div class=""button-container"">
                                        <a href=""[swiftlinelink]"" class=""button"">CHECK MY STATUS</a>
                                    </div>
            
                                    <p>Thank you for your patience. We'll see you soon!</p>
            
                                    <p>The Swiftline Team</p>
                                </div>
        
                                <!-- Footer -->
                                <div class=""footer"">
                                    <div class=""footer-links"">
                                        <a href=""#"">Help Center</a>
                                        <a href=""#"">Privacy Policy</a>
                                        <a href=""#"">Terms of Service</a>
                                    </div>
            
                                    <p>&copy; 2025 Swiftline. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";

                                
        }
    }
}
