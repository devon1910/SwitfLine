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
            // 1. Fetch all relevant lines + line members in one query
            var othersInLines = await dbContext.Lines
                .Where(x => !x.IsAttendedTo && x.IsActive && x.LineMember.EventId == line.LineMember.EventId)
                .Include(x => x.LineMember)
                .AsSplitQuery()
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            // 2. Get current user info and notify
            var userId = line.LineMember.UserId;
            var lineInfo = await lineRepo.GetUserLineInfo(userId);
            var notifyTasks = new List<Task>
                {
                    notifierHub.NotifyUserPositionChange(userId, lineInfo)
                };
    
            // 3. Batch fetch line info for all others (assuming you can optimize this inside the repo)
            foreach (var l in othersInLines)
            {
                var otherUserId = l.LineMember.UserId;
                notifyTasks.Add(Task.Run(async () =>
                {
                    var info = await lineRepo.GetUserLineInfo(otherUserId);
                    await notifierHub.NotifyUserPositionChange(otherUserId, info);
                }));
            }

            // 4. Await all notifications concurrently
            await Task.WhenAll(notifyTasks);

            // 5. Efficient event state update
            if (!othersInLines.Any())
            {
                // Use the already-known EventId to fetch only IsActive if needed
                var isEventActive = await dbContext.Events
                    .Where(e => e.Id == line.LineMember.EventId)
                    .Select(e => e.IsActive)
                    .FirstOrDefaultAsync();

                if (!isEventActive)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE public.\"Events\" SET \"IsActive\" = TRUE WHERE \"Id\" = {line.LineMember.EventId}");
                }
            }

        }

    }
}
