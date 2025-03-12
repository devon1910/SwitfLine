using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
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

        public ClaimsPrincipal VerifyToken(string token);

    }

    public interface IAuthService
    {
        public Task<Result<AuthRes>> SignUp(SignupModel model);
        public Task<Result<AuthRes>> Login(LoginModel model);
        public Task<Result<AuthRes>> RefreshToken(TokenModel tokenModel);
        public Task<Result<bool>> Revoke(ClaimsPrincipal User);

        public Result<bool> VerifyToken(string token);
    }
}
