using Application.Services;
using Azure.Core;
using Domain.AttributeValidator;
using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftLine.API.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SwiftLine.API.Controllers
{
    public class AuthController(IAuthService service) : BaseController
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
        public ActionResult<Result<bool>> VerifyToken([JwtTokenAttribute] string token)
        {
            var res = service.VerifyToken(token);
            return res.ToActionResult();
        } 
       
    }
}
