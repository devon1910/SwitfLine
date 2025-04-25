using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;

namespace SwiftLine.API.Controllers
{
    public class FeedbackController(IFeedbackService service) : BaseController
    {
        [HttpPost(), AllowAnonymous]
        public ActionResult<Result<bool>> SubmitFeedback(SubmitFeedbackModel model)
        {
            model.UserId = UserId;
            var res = service.SubmitFeedback(model);
            return res.ToActionResult();
        }
    }
}
