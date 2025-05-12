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
        public async Task<Result<LineInfoRes>> GetUserLineInfo(string UserId)
        {
            var lineInfo = await lineRepo.GetUserLineInfo(UserId);

            return Result<LineInfoRes>.Ok(lineInfo);
        }
        
    }
}
