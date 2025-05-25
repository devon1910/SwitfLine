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
    [Authorize(Roles = $"{Roles.Admin},{Roles.User}")]
    public class EventController(IEventService eventService) : BaseController
    {
        [HttpPost()]
        public async Task<ActionResult<Result<bool>>> CreateEvent(CreateEventModel request)
        {
            
            var res = await eventService.CreateEvent(UserId,request);
            return res.ToActionResult();
        }

      
        [HttpPut]
        public async Task<ActionResult<Result<bool>>> EditEvent(EditEventReq req)
        {
            var res = await eventService.EditEvent(req);
            return res.ToActionResult();
        }
       
        [HttpGet()]
        public async Task<ActionResult<Result<EventQueueRes>>> GetEventQueue(int CurrentMembersPage,int PastMembersPage, int Size, long EventId)
        {
            var res = await eventService.GetEventQueue(CurrentMembersPage, PastMembersPage, Size,EventId);
            return res.ToActionResult();
        }
        [HttpGet]
        public async Task<ActionResult<Result<GetUserEventsRes>>> GetUserEvents()
        {
            var res = await eventService.GetUserEvents(UserId);
            return res.ToActionResult();
        }
        
        [HttpDelete("{Id}")]
        public async Task<ActionResult<Result<bool>>> DeleteEvent(long Id)
        {
            var res =eventService.DeleteEvent(Id);
            return res.ToActionResult();
        }

        [HttpGet, AllowAnonymous]
        public async Task<ActionResult<Result<SearchEventsRes>>> SearchEvents(int Page, int Size, string Query="")
        {
           var res = await eventService.SearchEvents(Page,Size,Query, UserId);

            return res.ToActionResult();
        }



    }
}
