using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
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

            var othersInLines= await  dbContext.Lines
                .AsSplitQuery()
                .Include(x=>x.LineMember)
                .ThenInclude(x=> x.Event)
                .Where(x => x.LineMember.EventId == line.LineMember.EventId && !x.IsAttendedTo && x.IsActive)
                .ToListAsync();

            int position = othersInLines.IndexOf(line);


            return new LineInfoRes(line.LineMemberId, position);


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
            if (line.DateStartedBeingAttendedTo != default) 
            {
                line.DateStartedBeingAttendedTo = DateTime.UtcNow.AddHours(1);
                await dbContext.SaveChangesAsync();
            }

            var diff = (DateTime.UtcNow.AddHours(1) - line.DateStartedBeingAttendedTo).TotalSeconds;

            if (diff >= line.LineMember.Event.AverageTimeToServe) return true;
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
