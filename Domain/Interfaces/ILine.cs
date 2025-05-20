using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ILineRepo
    {
        public Task<bool> IsItUserTurnToBeServed(Line line, int EventAverageWaitSeconds);  
        public Task<bool> MarkUserAsServed(Line line,string status);
        public Task<LineInfoRes> GetUserLineInfo(string UserId);
        public Task<List<Line?>> GetFirstLineMembers(long eventId,int numberOfStaffServing);
        public Task Notify2ndLineMember(Line line);
    }
    public interface ILineService
    {
        public Task<Result<LineInfoRes>> GetUserLineInfo(string UserId);

    }
}
