using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SwiftLine.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    public class BaseController : ControllerBase
    {
        protected string UserId => "34f1a1a9-1a20-47bf-99c1-25d235b882d1";//User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
