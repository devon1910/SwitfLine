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
                return Result<AuthRes>.Failed("Failed to Refresh Token", refreshTokenRes);
            }
        }

        public async Task<Result<bool>> Revoke(ClaimsPrincipal User)
        {
            var revokeRes = await authRepo.Revoke(User);

            if (revokeRes)
            {
                return Result<bool>.Created(revokeRes,"Log out was Successful");
            }
            else
            {
                return Result<bool>.Failed("Failed to Log out", revokeRes);
            }
        }

        public async Task<Result<AuthRes>> VerifyToken(string token)
        {
            var authRes = await authRepo.VerifyToken(token);

            if (authRes.purpose != "Email_Verification")
            {
                return Result<AuthRes>.Failed("Invalid token purpose", authRes);
            }

            return Result<AuthRes>.Ok(authRes);
        }

        public async Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal)
        {
            var loginRes = await authRepo.LoginWithGoogleAsync(claimsPrincipal);
       
            return Result<string>.Ok(loginRes);
        }

        public async Task<Result<TurnstileResponse>> VerifyTurnstile(TurnstileModel request)
        {
            var turnstileRes = await authRepo.VerifyTurnstile(request);

            if (turnstileRes.success)
            {
                return Result<TurnstileResponse>.Ok(turnstileRes);
            }
            else
            {
                return Result<TurnstileResponse>.Failed("Failed to verify turnstile", turnstileRes);
            }
        }

        public Result<AuthRes> GetAuthData(string authCode)
        {
            var result = authRepo.GetAuthData(authCode);

            if (result.status)
            {
                return Result<AuthRes>.Ok(result);
            }
            else
            {
                return Result<AuthRes>.Failed("Failed to get auth data", result);
            }
        }
    }
}
