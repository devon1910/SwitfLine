using Application.Services;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;

namespace SwiftLine.API.Controllers
{
    public class QueueController(ILineService lineService) : BaseController
    {
        [HttpGet("/{LineMemberId}")]
        public async Task<ActionResult<Result<LineInfoRes>>> GetLineInfo(long LineMemberId)
        {
            var res = await lineService.GetLineInfo(LineMemberId);
            return res.ToActionResult();
        }

    }
}
