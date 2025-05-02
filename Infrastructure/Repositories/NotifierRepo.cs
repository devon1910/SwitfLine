using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class NotifierRepo(SwiftLineDatabaseContext dbContext, ILineRepo lineRepo, INotifier notifierHub) : INotifierRepo
    {
        public async Task BroadcastLineActivity(long eventId, bool status)
        {
            var othersInLines = await dbContext.Lines
                .Where(x => !x.IsAttendedTo && x.IsActive)
                .Include(x => x.LineMember)
                .AsSplitQuery()
                .Where(x => x.LineMember.EventId == eventId)
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();       

            foreach (var line in othersInLines)
            {
                string userId = line.LineMember.UserId;
                await notifierHub.NotifyUserQueueStatusUpdate(userId, status);
            }

           
        }

        public async Task BroadcastLineUpdate(Line line)
        {
            var othersInLines = await dbContext.Lines
                 .Where(x => !x.IsAttendedTo && x.IsActive && x.LineMember.EventId == line.LineMember.EventId)
                 .Include(x => x.LineMember)
                 .AsSplitQuery()
                 .OrderBy(x => x.CreatedAt)
                 .AsNoTracking()
                 .ToListAsync();

            var userId = line.LineMember.UserId;
            var lineInfo = await lineRepo.GetUserLineInfo(userId);
            await notifierHub.NotifyUserPositionChange(userId, lineInfo);

            var @event = await dbContext.Events.FindAsync(line.LineMember.EventId);

            foreach (var l in othersInLines)
            {
                var otherUserId = l.LineMember.UserId;
                var otherLineInfo = await lineRepo.GetUserLineInfo(otherUserId);
                await notifierHub.NotifyUserPositionChange(otherUserId, otherLineInfo);
            }

            if (!othersInLines.Any() && @event != null && !@event.IsActive)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
              $"UPDATE public.\"Events\" set \"IsActive\"=\'true\' where \"Id\"={@event.Id}");
            }

        }

    }
}
