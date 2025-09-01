using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;

namespace Application.Services
{
    public class LineService(ILineRepo lineRepo) : ILineService
    {
        public async Task<Result<LineInfoRes>> GetUserLineInfo(string UserId)
        {
            var lineInfo = await lineRepo.GetUserLineInfo(UserId);

            return Result<LineInfoRes>.Ok(lineInfo);
        }
        public async Task<Result<List<WordChainGameLeaderboard>>> GetTop10Players()
        {
            return Result<List<WordChainGameLeaderboard>>.Ok(await lineRepo.GetTop10Players());

        }

        public async Task<Result<bool>> UpdateUserScore(string UserId, LeaderboardUpdateReq req)
        {
            return Result<bool>.Ok(await lineRepo.UpdateUserScore(UserId, req));
        }

        
    }
}
