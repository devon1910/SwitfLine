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
                 .Where(x => !x.IsAttendedTo && x.IsActive)
                 .Include(x => x.LineMember)
                 .AsSplitQuery()
                 .Where(x => x.LineMember.EventId == line.LineMember.EventId)
                 .OrderBy(x => x.CreatedAt)
                 .AsNoTracking()
                 .ToListAsync();

            string userId = line.LineMember.UserId;
            var lineinfo = await lineRepo.GetUserLineInfo(userId);
            await notifierHub.NotifyUserPositionChange(userId, lineinfo);

            Event @event = dbContext.Events.Find(line.LineMember.EventId);

            
            foreach (var l in othersInLines)
            {
                userId = l.LineMember.UserId;
                lineinfo = await lineRepo.GetUserLineInfo(userId);
                await notifierHub.NotifyUserPositionChange(userId, lineinfo);
            }

            if (othersInLines.Count == 0 && !@event.IsActive)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
               $"UPDATE public.\"Events\" set \"IsActive\"=\'true\' where \"Id\"={@event.Id}");
            }

            //var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };
            //await notifierHub.UpdateIsUserInQueue(line.LineMember.SwiftLineUser.Id);
            //await Parallel.ForEachAsync(othersInLines, parallelOptions, async (l, token) =>
            //{
            //    //string userId = l.LineMember.SwiftLineUser.Id;
            //    //using var dbContext = _dbContextFactory.CreateDbContext();

            //    //// Create a new repository instance (or pass the new context to your method)
            //    //var newLineRepo = new LineRepository(dbContext);

            //    //var lineinfo = await newLineRepo.GetUserLineInfo(userId);

            //    //I'll come back to this later, i Think I need to refactor whereby i use a dbcontext to manage the contexts lifecycle
            //    var lineinfo = await lineRepo.GetUserLineInfo(userId);
            //    await notifierHub.NotifyUserPositionChange(userId, lineinfo);
            //});
        }

    }
}
