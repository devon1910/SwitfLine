using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class AuthRepo(UserManager<SwiftLineUser> _userManager,
                                RoleManager<IdentityRole> _roleManager,
                                SwiftLineDatabaseContext _context, ITokenRepo _tokenService, ILogger<AuthRepo> _logger) : IAuthRepo
    {
 
        public async Task<AuthRes> Signup(SignupModel model)
        {
            var existingUser = await _userManager.FindByNameAsync(model.Email);
            if (existingUser != null)
            {
                return new AuthRes(false,"User already exists","","","","",false);
            }

            // Create User role if it doesn't exist
            if ((await _roleManager.RoleExistsAsync(Roles.User)) == false)
            {
                var roleResult = await _roleManager
                      .CreateAsync(new IdentityRole(Roles.User));

                if (!roleResult.Succeeded)
                {
                    var roleErros = roleResult.Errors.Select(e => e.Description);
                    _logger.LogError($"Failed to create user role. Errors : {string.Join(",", roleErros)}");
                    return new AuthRes(false,$"Failed to create user role. Errors : {string.Join(",", roleErros)}","","","","", false);
                }
            }

            SwiftLineUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email,
                EmailConfirmed = true
            };

            // Attempt to create a user
            var createUserResult = await _userManager.CreateAsync(user, model.Password);

            // Validate user creation. If user is not created, log the error and
            // return the BadRequest along with the errors
            if (!createUserResult.Succeeded)
            {
                var errors = createUserResult.Errors.Select(e => e.Description);
                _logger.LogError(
                    $"Failed to create user. Errors: {string.Join(", ", errors)}"
                );
                return new AuthRes(false, $"Failed to create user. Errors: {string.Join(", ", errors)}", "", "","", "", false);
            }

            // adding role to user
            var addUserToRoleResult = await _userManager.AddToRoleAsync(user: user, role: Roles.User);

            if (addUserToRoleResult.Succeeded == false)
            {
                var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                _logger.LogError($"Failed to add role to the user. Errors : {string.Join(",", errors)}");
            }
            return new(true, "User Created", "", "", user.Id, user.Email,user.IsInQueue);

        }

        public async Task<AuthRes> Login(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            bool isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (user is null || !isValidPassword)
            {
                return new AuthRes(false, "Invalid User name or password.", "", "", "", "",false);
            }

            // creating the necessary claims
            List<Claim> authClaims = [
                    new (ClaimTypes.Name, user.UserName),
                        new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new (ClaimTypes.NameIdentifier, user.Id),
                        // unique id for token
                        ];

            var userRoles = await _userManager.GetRolesAsync(user);

            // adding roles to the claims. So that we can get the user role from the token.
            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            // generating access token
            var token = _tokenService.GenerateAccessToken(authClaims);

            string refreshToken = _tokenService.GenerateRefreshToken();

            //save refreshToken with exp date in the database
            var tokenInfo = _context.TokenInfos.
                        FirstOrDefault(a => a.Username == user.UserName);

            // If tokenInfo is null for the user, create a new one
            if (tokenInfo == null)
            {
                var ti = new TokenInfo
                {
                    Username = user.UserName,
                    RefreshToken = refreshToken,
                    ExpiredAt = DateTime.UtcNow.AddHours(1).AddDays(7)
                };
                _context.TokenInfos.Add(ti);
            }
            // Else, update the refresh token and expiration
            else
            {
                tokenInfo.RefreshToken = refreshToken;
                tokenInfo.ExpiredAt = DateTime.UtcNow.AddDays(7);
            }

            await _context.SaveChangesAsync();

            return new AuthRes(true, "Login Successful", token, refreshToken, user.Id,user.Email,user.IsInQueue);
           
        }

        public async Task<AuthRes> RefreshToken(TokenModel tokenModel)
        {

            var principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            var username = principal.Identity.Name;

            var tokenInfo = _context.TokenInfos.SingleOrDefault(u => u.Username == username);
            if (tokenInfo == null
                || tokenInfo.RefreshToken != tokenModel.RefreshToken
                || tokenInfo.ExpiredAt <= DateTime.UtcNow)
            {
                return new AuthRes(false,"Invalid refresh token. Please login again.","","", "", "", false);
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            tokenInfo.RefreshToken = newRefreshToken; // rotating the refresh token
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByNameAsync(username); 

            return new AuthRes(true, "Refresh token updated.", newAccessToken, newRefreshToken,user.Id,user.Email, user.IsInQueue);
           
        }

        public async Task<bool> Revoke(ClaimsPrincipal User)
        {
            var username = User.Identity.Name;

            var user = _context.TokenInfos.SingleOrDefault(u => u.Username == username);
            if (user is null)
            {
                return false;
            }

            user.RefreshToken = string.Empty;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
