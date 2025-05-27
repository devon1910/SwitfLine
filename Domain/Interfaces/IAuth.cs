using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAuthRepo
    {
        public Task<AuthRes> Signup(SignupModel model);
        public Task<AuthRes> Login(LoginModel model);
        public Task<AuthRes> RefreshToken(TokenModel tokenModel);
        public Task<bool> Revoke(ClaimsPrincipal User);
        public AuthRes VerifyToken(string token);
        public Task<string> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal);
        public Task<TurnstileResponse> VerifyTurnstile(TurnstileModel request);
        public Task<AnonymousUserAuthRes> CreateAnonymousUser();
        public Task<List<SwiftLineUser>> GetExpiredAccounts();
        public Task DeleteExpiredAccount(SwiftLineUser user);

        public AuthRes GetAuthData(string authCode);
    }

    public interface IAuthService
    {
        public Task<Result<AuthRes>> SignUp(SignupModel model);
        public Task<Result<AuthRes>> Login(LoginModel model);
        public Task<Result<AuthRes>> RefreshToken(TokenModel tokenModel);
        public Task<Result<bool>> Revoke(ClaimsPrincipal User);
        public Result<AuthRes> VerifyToken(string token);
        public Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal);
        public Task<Result<TurnstileResponse>> VerifyTurnstile(TurnstileModel request);

        public Result<AuthRes> GetAuthData(string authCode);
    }
}
