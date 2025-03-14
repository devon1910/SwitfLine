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
        public Task<Result<List<Line>>> GetLines()
        {
            throw new NotImplementedException();
        }

        public async Task<Result<LineInfoRes>> GetUserLineInfo(string UserId)
        {
            var lineInfo = await lineRepo.GetUserLineInfo(UserId);

            return Result<LineInfoRes>.Ok(lineInfo);
        }

        public Result<bool> GetUserQueueStatus(string UserId)
        {
            var lineQueuestatus = lineRepo.GetUserQueueStatus(UserId);

            return Result<bool>.Ok(lineQueuestatus);
        }

        
    }
}
