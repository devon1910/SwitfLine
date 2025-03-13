using Application.Services;
using Azure.Core;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
                1 => number+ "st",
                2 => number+ "nd",
                3 => number+ "rd",
                _ => number+ "th",
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

        public async Task<bool> MarkUserAsAttendedTo(Line line)
        {
           line.IsAttendedTo = true;
           line.DateCompletedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
           line.LineMember.SwiftLineUser.IsInQueue = false;
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Line?> GetFirstLineMember(long eventId)
        {
            return await  dbContext.Lines
                .Where(x => x.IsActive && !x.IsAttendedTo)
                .Include(x => x.LineMember)
                .Include(x => x.LineMember.Event)
                .Include(x => x.LineMember.SwiftLineUser)
                .AsSplitQuery()
                .Where(x => x.LineMember.EventId == eventId)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();      
        }

        public async Task<bool> ServeUser(long lineMemberId)
        {
            Line line = dbContext.Lines.FirstOrDefault(x => x.LineMemberId == lineMemberId);

            return await MarkUserAsAttendedTo(line);
        }

        public async Task<LineInfoRes> GetUserLineInfo(string UserId)
        {

                var line = await  dbContext.Lines
                .AsSplitQuery()
                .Where(x => !x.IsAttendedTo && x.IsActive)
                .Include(x => x.LineMember)
                .ThenInclude(x => x.Event)
                .Where(x => x.LineMember.UserId == UserId)
                .FirstOrDefaultAsync();

                if(line is null) return new LineInfoRes(0, -1, 0, "", "");

                int position = 0;
                var othersInLines = await dbContext.Lines
                       .Where(x => x.IsActive && !x.IsAttendedTo)
                       .Include(x => x.LineMember)
                       .AsSplitQuery()
                       .Where(x => x.LineMember.EventId == line.LineMember.EventId)
                       .ToListAsync();

                position = othersInLines.IndexOf(line) + 1;

                int timeTillYourTurn = ((line.LineMember.Event.AverageTimeToServeSeconds * position) - line.LineMember.Event.AverageTimeToServeSeconds) / 60;
                //+ GetOrdinal(position)
                return new LineInfoRes(line.LineMemberId, position, timeTillYourTurn, GetOrdinal(position), line.LineMember.Event.Title);  
        }

        public bool GetUserQueueStatus(string UserId)
        {
            var user=  dbContext.SwiftLineUsers.Find( UserId);
            return user.IsInQueue;
            
        }

        public async Task NotifyFifthMember(long eventId)
        {
            var user = await dbContext.Lines
                .Where(x => x.IsActive && !x.IsAttendedTo)
                .Include(x => x.LineMember)
                .Include(x => x.LineMember.Event)
                .Include(x => x.LineMember.SwiftLineUser)
                .AsSplitQuery()
                .Where(x => x.LineMember.EventId == eventId)
                .OrderBy(x => x.CreatedAt)
                .Skip(4)
                .FirstOrDefaultAsync();

            int EstimatedTime = (user.LineMember.Event.AverageTimeToServeSeconds * 5) / 60;
            if (user is not null) 
            {
                SendReminderMail(user.LineMember.SwiftLineUser.Email, EstimatedTime);
            }

        }
        private async Task<bool> SendReminderMail(string RecipientEmail, int EstimatedTime)
        {
            string htmlTemplate = GetEmailTemplate();
            string link = _configuration["SwiftLineBaseUrl"]; 
            var email = await _fluentEmail
                .To(RecipientEmail)
                .Subject($"Welcome to Swiftline⚡ - Verify Your Email Address")
                .Body(htmlTemplate
                .Replace("[UserName]", RecipientEmail)
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
                            </style>
                        </head>
                        <body>
                            <div class=""container"">
                                <!-- Header -->
                                
        
                                <!-- Main Content -->
                                <div class=""content"">
                                    <h1>You're Almost Up!</h1>
            
                                    <p>Hello [UserName],</p>
            
                                    <p>Great news! Your turn in the queue is coming up very soon. Please make sure you're ready and stay nearby.</p>
            
                                    <div class=""timer-container"">
                                        <div class=""time-label"">Estimated time until your turn:</div>
                                        <div class=""estimated-time"">[estimatedTime]</div>
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

            //< div class=""header"">
            //                        <!-- Replace with actual logo image URL -->
            //                        <img src = ""https://example.com/swiftline-logo.png"" alt=""Swiftline"" class=""logo"">
            //                    </div>
        }
    }
}
