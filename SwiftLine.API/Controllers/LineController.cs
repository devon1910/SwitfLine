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
    public class LineController(ILineService lineService) : BaseController
    {
        [HttpGet()]
        public async Task<ActionResult<Result<LineInfoRes>>> GetUserLineInfo()
        {
            var res = await lineService.GetUserLineInfo(UserId);
            return res.ToActionResult();
        }  
        [HttpGet()]
        public ActionResult<Result<bool>> GetUserQueueStatus()
        {
            var res =  lineService.GetUserQueueStatus(UserId);
            return res.ToActionResult();
        } 
        [HttpPost("{LineMemberId}")]
        public async Task<ActionResult<Result<bool>>> ExitQueue(long LineMemberId)
        {
            var res = await lineService.ExitQueue(LineMemberId);
            return res.ToActionResult();
        }
    }
}
