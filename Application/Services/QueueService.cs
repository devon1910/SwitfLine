using Domain.DTOs.Responses;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class QueueService(IQueueRepo queueRepo) : IQueueService
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
    }
}
