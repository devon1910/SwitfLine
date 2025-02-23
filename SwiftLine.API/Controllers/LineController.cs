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
        [HttpGet("{LineMemberId}")]
        public async Task<ActionResult<Result<LineInfoRes>>> GetLineInfo(long LineMemberId)
        {
            var res = await lineService.GetLineInfo(LineMemberId);
            return res.ToActionResult();
        } 
        [HttpPost("{LineMemberId}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<Result<bool>>> ServeUser(long LineMemberId)
        {
            var res = await lineService.ServeUser(LineMemberId);
            return res.ToActionResult();
        }

    }
}
