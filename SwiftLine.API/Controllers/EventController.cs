using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;

namespace SwiftLine.API.Controllers
{
    public class EventController(IEventService eventService) : BaseController
    {
        [HttpPost]
        public async Task<ActionResult<Result<bool>>> CreateEvent([FromBody] CreateEventReq request)
        {
            
            var res = await eventService.CreateEvent(UserId,request);
            return res.ToActionResult();
        }
    }
}
