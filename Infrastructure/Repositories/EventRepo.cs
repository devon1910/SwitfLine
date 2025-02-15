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
    }
}
