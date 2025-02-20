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
        public async Task<LineInfoRes> GetLineInfo(long LineMemberId)
        {
            var line = dbContext.Lines
                .AsSplitQuery()
                .Where(x=>x.LineMemberId==LineMemberId && !x.IsAttendedTo)
                .Include(x=>x.LineMember)
                .ThenInclude(x=> x.Event)
                .FirstOrDefault();

            if (line is null) return new LineInfoRes(LineMemberId, "0th","");

            int position = 0;
            var othersInLines = await dbContext.Lines
                   .AsSplitQuery()
                   .Include(x => x.LineMember)
                   .ThenInclude(x => x.Event)
                   .Where(x => x.LineMember.EventId == line.LineMember.EventId && !x.IsAttendedTo && x.IsActive)
                   .ToListAsync();

            position = othersInLines.IndexOf(line) + 1;

            var timeTillYourTurn = ((line.LineMember.Event.AverageTimeToServeSeconds * position) - line.LineMember.Event.AverageTimeToServeSeconds)/60;

            return new LineInfoRes(line.LineMemberId, $"{position}" + GetOrdinal(position), $"{timeTillYourTurn} minutes");


        }

        private string GetOrdinal(int number)
        {
            int lastTwo = number % 100;
            if (lastTwo >= 11 && lastTwo <= 13) return "th";

            return (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
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
            await dbContext.SaveChangesAsync();
           return true;
        }

        public async Task<Line?> GetFirstLineMember(long eventId)
        {
            return await  dbContext.Lines
                .Include(x => x.LineMember)
                .ThenInclude(x => x.Event)
                .Where(x => x.LineMember.EventId == eventId && !x.IsAttendedTo)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            
        }
    }
}
