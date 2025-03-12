using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AuthService(IAuthRepo authRepo) : IAuthService
    {
        public async Task<Result<AuthRes>> Login(LoginModel model)
        {
            var loginRes= await authRepo.Login(model);

            if (loginRes.status) 
            {
                return Result<AuthRes>.Ok(loginRes);
            }
            else 
            {
                return Result<AuthRes>.Failed("Failed to Login", loginRes);
            }
        }

       

        public async Task<Result<AuthRes>> SignUp(SignupModel model)
        {
            var signUpRes = await authRepo.Signup(model);

            if (signUpRes.status)
            {
                return Result<AuthRes>.Created(signUpRes);
            }
            else
            {
                return Result<AuthRes>.Failed("Failed to Signup",signUpRes);
            }
        }

        public async Task<Result<AuthRes>> RefreshToken(TokenModel tokenModel)
        {
            var refreshTokenRes = await authRepo.RefreshToken(tokenModel);

            if (refreshTokenRes.status)
            {
                return Result<AuthRes>.Created(refreshTokenRes);
            }
            else
            {
                return Result<AuthRes>.Failed("Failed to Signup", refreshTokenRes);
            }
        }

        public async Task<Result<bool>> Revoke(ClaimsPrincipal User)
        {
            var revokeRes = await authRepo.Revoke(User);

            if (revokeRes)
            {
                return Result<bool>.Created(revokeRes);
            }
            else
            {
                return Result<bool>.Failed("Failed to Signup", revokeRes);
            }
        }

        public Result<bool> VerifyToken(string token)
        {
            var principal = authRepo.VerifyToken(token);
            string userId = principal.FindFirst("userId")?.Value;
            string email = principal.FindFirst("email")?.Value;
            string purpose = principal.FindFirst("purpose")?.Value;

            if (purpose != "Email_Verification")
            {
                return Result<bool>.Failed("Invalid token purpose",false);
            }

            return Result<bool>.Ok(true);
        }

    }
}
