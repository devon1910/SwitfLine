using Application.Services;
using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;

namespace SwiftLine.API.Controllers
{
    //Can I convert these into minimal APIS?
    public class LineController(ILineService lineService) : BaseController
    {
        [HttpGet()]
        public async Task<ActionResult<Result<LineInfoRes>>> GetUserLineInfo()
        {
            var res = await lineService.GetUserLineInfo(UserId);
            return res.ToActionResult(); 
        }  

        //endpoint to get the top 10 players
        [HttpGet()]
        public async Task<ActionResult<Result<List<WordChainGameLeaderboard>>>> Top10players()
        {
            var res = await lineService.GetTop10Players();
            return res.ToActionResult();
        }
        //endpoint to update the user score
        [HttpPut()]
        public async Task<ActionResult<Result<bool>>> UpdateUserScore(LeaderboardUpdateReq req)
        {
            var res = await lineService.UpdateUserScore(UserId, req);
            return res.ToActionResult();
        }

    }
}
