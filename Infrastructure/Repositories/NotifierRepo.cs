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
    public class NotifierRepo(SwiftLineDatabaseContext dbContext, ILineRepo lineRepo, INotifier notifierHub) : INotifierRepo
    {
        public async Task BroadcastLineUpdate(Line line)
        {
            /// Notify the user that they have been attended to
            var othersInLines = await dbContext.Lines
                 .Where(x => !x.IsAttendedTo && x.IsActive)
                 .Include(x => x.LineMember)
                 .Include(x => x.LineMember.Event)
                 .Include(x=>x.LineMember.SwiftLineUser)
                 .AsSplitQuery()
                 .Where(x => x.LineMember.EventId == line.LineMember.EventId)
                 .AsNoTracking()
                 .ToListAsync();

            //foreach (var l in othersInLines)
            //{
            //    var lineinfo = await GetLineInfo(l.LineMemberId);
            //    await notifierHub.NotifyUserPositionChange(l.LineMember.SwiftLineUser.Id, lineinfo);
            //}

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };
            await Parallel.ForEachAsync(othersInLines, parallelOptions, async (l, token) =>
            {
                var lineinfo = MiniGetLineInfores(othersInLines, l);

                await notifierHub.NotifyUserPositionChange(l.LineMember.SwiftLineUser.Id, lineinfo);
            });
        }

        private LineInfoRes MiniGetLineInfores(List<Line> othersInLines,Line line)
        {
            int position = othersInLines.IndexOf(line) + 1;

            var timeTillYourTurn = ((line.LineMember.Event.AverageTimeToServeSeconds * position) - line.LineMember.Event.AverageTimeToServeSeconds) / 60;
            //+ GetOrdinal(position)
            return new LineInfoRes(line.LineMemberId, $"{position}", timeTillYourTurn, line.LineMember.EventId);


        }
    }
}
