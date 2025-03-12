using Application.Services;
using Azure.Core;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class LineRepo(SwiftLineDatabaseContext dbContext) : ILineRepo
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
            if (line.DateStartedBeingAttendedTo == default) 
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
    }
}
