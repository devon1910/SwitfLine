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
        public Task<List<Line>> GetLines();
        public Task<bool> IsUserAttendedTo(Line line);  
        public Task<bool> MarkUserAsAttendedTo(Line line);
        //public Task<LineInfoRes> GetLineInfo(long LineMemberId);
        public Task<LineInfoRes> GetUserLineInfo(string UserId);

        public bool GetUserQueueStatus(string UserId);

        public Task<Line?> GetFirstLineMember(long eventId);
        public Task<bool> ServeUser(long lineMemberId);

        public Task NotifyFifthMember(long eventId);
    }
    public interface ILineService
    {
        public Task<Result<List<Line>>> GetLines();

        //public Task<Result<LineInfoRes>> GetLineInfo(long LineMemberId);

        public Task<Result<LineInfoRes>> GetUserLineInfo(string UserId);

        public Result<bool> GetUserQueueStatus(string UserId);

        public Task<Result<bool>> ServeUser(long lineMemberId);
    }
}
