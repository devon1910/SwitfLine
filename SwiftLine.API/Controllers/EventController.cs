using Application.Services;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;

namespace SwiftLine.API.Controllers
{
    public class EventController(IEventService eventService) : BaseController
    {
        [HttpPost("api/v1/[controller]")]
        public async Task<ActionResult<Result<bool>>> CreateEvent(CreateEventReq request)
        {
            
            var res = await eventService.CreateEvent(UserId,request);
            return res.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult<Result<bool>>> JoinEvent(long EventId)
        {

            var res = await eventService.JoinEvent(UserId, EventId);
            return res.ToActionResult();
        }

        [HttpPut("api/v1/[controller]")]
        public async Task<ActionResult<Result<bool>>> EditEvent(EditEventReq req)
        {
            var res = await eventService.EditEvent(req);
            return res.ToActionResult();
        }
        [HttpGet("api/v1/[controller]/{EventId}")]
        public async Task<ActionResult<Result<Event>>> GetEvent(long EventId)
        {

            var res = await eventService.GetEvent(EventId);
            return res.ToActionResult();
        }
    }
}
