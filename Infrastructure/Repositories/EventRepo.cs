using Domain.DTOs.Requests;
using Domain.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    
    public class EventRepo(SwiftLineDatabaseContext dbContext) : IEventRepo
    {
        public async Task<bool> CreateEvent(string userId, CreateEventReq req)
        {
            var newEvent = new Event
            {
                Name = req.Name,
                AverageTimeToServe = req.AverageTimeToServe * 60,
                CreatedBy = userId
            };
            await dbContext.Events.AddAsync(newEvent);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> JoinEvent(string userId, long eventId)
        {
            var newQueueMember = new QueueMember
            {
                EventId = eventId,
                UserId = userId
            };

            await dbContext.QueueMembers.AddAsync(newQueueMember);
            await dbContext.SaveChangesAsync();

            Queue queue = new()
            {
                QueueMemberId = newQueueMember.Id
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}
