using Domain.AttributeValidator;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SwiftLine.API.Extensions;
using Microsoft.AspNetCore.RateLimiting;

namespace SwiftLine.API.Controllers
{
    public class AuthController(IAuthService service, IConfiguration _config, LinkGenerator _lineGenerator, SignInManager<SwiftLineUser> _signInManager) : BaseController
    {
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("SignupPolicy")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<Result<AuthRes>>> Signup(SignupModel model)
        {
            var res = await service.SignUp(model);
            return res.ToActionResult();
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")]

        public async Task<ActionResult<Result<AuthRes>>> Login(LoginModel model)
        {
            var res = await service.Login(model);
            return res.ToActionResult();
        }

        [HttpPost, AllowAnonymous]
        public async Task<ActionResult<Result<AuthRes>>> RefreshToken(TokenModel tokenModel)
        {
            var res = await service.RefreshToken(tokenModel);
            return res.ToActionResult();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Result<AuthRes>>> VerifyToken([JwtTokenAttribute] string token)
        {
            var res = await service.VerifyToken(token);
            return res.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult<Result<bool>>> Logout()
        {
            var res = await service.Revoke(User);
            return res.ToActionResult();
        }

        [HttpGet(), AllowAnonymous]
        public IActionResult LoginWithGoogle()
        {
            string returnUrl = _config["SwiftLineBaseUrl"];

            var callbackUrl = _lineGenerator.GetPathByName(
                HttpContext,
                "GoogleLoginCallback",
                values: new { returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                "Google",
                $"{callbackUrl}");

            return Challenge(properties, "Google");
        }

        [HttpGet(Name = "GoogleLoginCallback"), AllowAnonymous]
        public async Task<IActionResult> GoogleLoginCallback([FromQuery] string returnUrl)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                return Unauthorized();
            }

            var result = await service.LoginWithGoogleAsync(authenticateResult.Principal);

            // Instead of using cookies, append tokens to the redirect URL:
            return Redirect($"{returnUrl}?authCode={result.Data}");

        }


        [HttpGet, AllowAnonymous]
        public ActionResult<Result<AuthRes>> GetAuthData(string authCode)
        {
            var res =  service.GetAuthData(authCode);
            return res.ToActionResult();
        }

        [HttpPost, AllowAnonymous]
        public async Task<ActionResult<Result<TurnstileResponse>>> VerifyTurnstileToken([FromBody] TurnstileModel request)
        {
            var res = await service.VerifyTurnstile(request);
            return res.ToActionResult();
        }
        
    }
}
