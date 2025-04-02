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
        [HttpGet(), AllowAnonymous]
        public async Task<ActionResult<Result<LineInfoRes>>> GetUserLineInfo()
        {
            var res = await lineService.GetUserLineInfo(UserId);
            return res.ToActionResult();
        }  
       
    }
}
