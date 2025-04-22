using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace SwiftLine.API.Controllers
{

    [Authorize]
    [ApiController]
    [EnableRateLimiting("GenericRestriction")]
    [Route("api/v1/[controller]/[action]")]
    public class BaseController : ControllerBase
    {
        protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    }
}
