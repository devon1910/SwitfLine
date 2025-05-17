using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;

namespace SwiftLine.API.Controllers
{
    public class PushNotificationController(IPushNotificationService pushNotificationService) : BaseController
    {
     
        [HttpPost]
        public async Task<ActionResult<Result<bool>>> Subscribe(string subscription)
        {
            var res = await pushNotificationService.Save(UserId, subscription);
            return res.ToActionResult();
        }
    }
}
