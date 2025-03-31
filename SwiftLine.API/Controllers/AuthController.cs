using Application.Services;
using Azure.Core;
using Domain.AttributeValidator;
using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SwiftLine.API.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace SwiftLine.API.Controllers
{
    public class AuthController(IAuthService service,IConfiguration _config, LinkGenerator _lineGenerator, SignInManager<SwiftLineUser> _signInManager) : BaseController
    {
        [HttpPost]
        [AllowAnonymous]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<Result<AuthRes>>> Signup(SignupModel model)
        {
            var res = await service.SignUp(model);
            return res.ToActionResult();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Result<AuthRes>>> Login(LoginModel model)
        {
            var res = await service.Login(model);
            return res.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult<Result<AuthRes>>> RefreshToken(TokenModel tokenModel)
        {
            var res = await service.RefreshToken(tokenModel);
            return res.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult<Result<bool>>> Revoke()
        {
            var res = await service.Revoke(User);
            return res.ToActionResult();
        } 
        
        [HttpPost]
        [AllowAnonymous]
        public ActionResult<Result<AuthRes>> VerifyToken([JwtTokenAttribute] string token)
        {
            var res = service.VerifyToken(token);
            return res.ToActionResult();
        }

        [HttpPost] //Add to UI later
        public async Task<ActionResult<Result<bool>>> Logout()
        {
            var res = await service.Revoke(User);
            return res.ToActionResult();
        }

        [HttpGet(),AllowAnonymous]
        public IActionResult LoginWithGoogle()
        {
            string returnUrl = _config["SwiftLineBaseUrlForReminder"];

            var callbackUrl = _lineGenerator.GetPathByName(
                HttpContext,
                "GoogleLoginCallback",
                values: new { returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                "Google",
                $"{callbackUrl}");

            return Challenge(properties, "Google");
        }

        [HttpGet(Name = "GoogleLoginCallback"),AllowAnonymous]
        public async Task<IActionResult> GoogleLoginCallback([FromQuery] string returnUrl)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                return Unauthorized();
            }

            var result= await service.LoginWithGoogleAsync(authenticateResult.Principal);
           


            // Instead of using cookies, append tokens to the redirect URL:
            return Redirect($"{returnUrl}?accessToken={result.Data.AccessToken}&refreshToken={result.Data.RefreshToken}&username={result.Data.userName}&userId={result.Data.userId}");

        }

    }
}
