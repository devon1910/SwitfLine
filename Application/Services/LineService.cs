using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LineService(ILineRepo lineRepo) : ILineService
    {
        //public async Task<Result<bool>> JoinEvent(string userId, long eventId)
        //{
        //    bool isJoined = await queueRepo.JoinEvent(userId, eventId);

        //    if (isJoined)
        //    {
        //        return Result<bool>.Ok(true);
        //    }
        //    return Result<bool>.Failed("Failed to join event");
        //}
        public async Task<Result<LineInfoRes>> GetLineInfo(long QueueMemberId)
        {
            var lineInfo = await lineRepo.GetLineInfo(QueueMemberId);

            return Result<LineInfoRes>.Ok(lineInfo);
        }

        public Task<Result<List<Line>>> GetLines()
        {
            throw new NotImplementedException();
        }


        public async Task<Result<bool>> ServeUser(long lineMemberId)
        {
            bool isServed = await lineRepo.ServeUser(lineMemberId);

            if (isServed)
            {
                return Result<bool>.Ok(true);
            }
            return Result<bool>.Failed("Failed to Attend to User.");
        }
    }
}
