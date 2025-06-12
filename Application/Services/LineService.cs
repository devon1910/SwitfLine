using Domain.DTOs.Responses;
using Domain.Interfaces;

namespace Application.Services
{
    public class LineService(ILineRepo lineRepo) : ILineService
    {
        public async Task<Result<LineInfoRes>> GetUserLineInfo(string UserId)
        {
            var lineInfo = await lineRepo.GetUserLineInfo(UserId);

            return Result<LineInfoRes>.Ok(lineInfo);
        }
        
    }
}
