using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SwiftLine.API.Controllers
{
    public class AuthController(UserManager<SwiftLineUser> _userManager,
                                RoleManager<IdentityRole> _roleManager, 
                                SwiftLineDatabaseContext _context, ITokenRepo _tokenService, ILogger<AuthController> _logger) : BaseController
    {
        [HttpPost]
        [AllowAnonymous]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Signup(SignupModel model)
        {
            try
            {
                var existingUser = await _userManager.FindByNameAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest("User already exists");
                }

                // Create User role if it doesn't exist
                if ((await _roleManager.RoleExistsAsync(Roles.User)) == false)
                {
                    var roleResult = await _roleManager
                          .CreateAsync(new IdentityRole(Roles.User));

                    if (roleResult.Succeeded == false)
                    {
                        var roleErros = roleResult.Errors.Select(e => e.Description);
                        _logger.LogError($"Failed to create user role. Errors : {string.Join(",", roleErros)}");
                        return BadRequest($"Failed to create user role. Errors : {string.Join(",", roleErros)}");
                    }
                }

                SwiftLineUser user = new()
                {
                    Email = model.Email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = model.Email,
                    Name = model.Name,
                    EmailConfirmed = true
                };

                // Attempt to create a user
                var createUserResult = await _userManager.CreateAsync(user, model.Password);

                // Validate user creation. If user is not created, log the error and
                // return the BadRequest along with the errors
                if (createUserResult.Succeeded == false)
                {
                    var errors = createUserResult.Errors.Select(e => e.Description);
                    _logger.LogError(
                        $"Failed to create user. Errors: {string.Join(", ", errors)}"
                    );
                    return BadRequest($"Failed to create user. Errors: {string.Join(", ", errors)}");
                }

                // adding role to user
                var addUserToRoleResult = await _userManager.AddToRoleAsync(user: user, role: Roles.User);

                if (addUserToRoleResult.Succeeded == false)
                {
                    var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                    _logger.LogError($"Failed to add role to the user. Errors : {string.Join(",", errors)}");
                }
                return CreatedAtAction(nameof(Signup), null);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    return BadRequest("User with this username is not registered with us.");
                }
                bool isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
                if (isValidPassword == false)
                {
                    return Unauthorized();
                }

                // creating the necessary claims
                List<Claim> authClaims = [
                        new (ClaimTypes.Name, user.UserName),
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
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

                return Ok(new TokenModel
                {
                    AccessToken = token,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Unauthorized();
            }

        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            try
            {
                var principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.AccessToken);
                var username = principal.Identity.Name;

                var tokenInfo = _context.TokenInfos.SingleOrDefault(u => u.Username == username);
                if (tokenInfo == null
                    || tokenInfo.RefreshToken != tokenModel.RefreshToken
                    || tokenInfo.ExpiredAt <= DateTime.UtcNow)
                {
                    return BadRequest("Invalid refresh token. Please login again.");
                }

                var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                tokenInfo.RefreshToken = newRefreshToken; // rotating the refresh token
                await _context.SaveChangesAsync();

                return Ok(new TokenModel
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Revoke()
        {
            try
            {
                var username = User.Identity.Name;

                var user = _context.TokenInfos.SingleOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return BadRequest();
                }

                user.RefreshToken = string.Empty;
                await _context.SaveChangesAsync();

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
