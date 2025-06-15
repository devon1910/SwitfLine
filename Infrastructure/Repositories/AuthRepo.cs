using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Numerics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Infrastructure.Repositories
{
    public class AuthRepo(UserManager<SwiftLineUser> _userManager,
                                RoleManager<IdentityRole> _roleManager,
                                SwiftLineDatabaseContext _context,
                                ITokenRepo _tokenService,
                                IEmailsDeliveryRepo emailsDeliveryRepo,
                                ILogger<AuthRepo> _logger,
                                IConfiguration _configuration) : IAuthRepo
    {

        public async Task<AuthRes> Signup(SignupModel model)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    string message = "";
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        message = existingUser.EmailConfirmed
                                ? "User already exists"
                                : "User already exists but email is not verified. Please check your email for the verification link and follow the instructions.";
                        return AuthResFailed.CreateFailed(message);
                    }

                    // Create User role if it doesn't exist
                    if (!(await _roleManager.RoleExistsAsync(Roles.User)))
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole(Roles.User));
                        if (!roleResult.Succeeded)
                        {
                            var roleErrors = roleResult.Errors.Select(e => e.Description);
                            _logger.LogError($"Failed to create user role. Errors: {string.Join(", ", roleErrors)}");
                            message = $"Failed to create user role. Errors: {string.Join(", ", roleErrors)}";
                            return AuthResFailed.CreateFailed(message);
                        }
                    }

                    // Create the new user
                    SwiftLineUser user = new()
                    {
                        Email = model.Email,
                        FullName = model.FullName,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        UserName = await GetUniqueUsername(model.Email),
                        EmailConfirmed = false,// just for now, verification later on.            
                    };

                    var createUserResult = await _userManager.CreateAsync(user, model.Password);
                    if (!createUserResult.Succeeded)
                    {
                        var errors = createUserResult.Errors.Select(e => e.Description);
                        _logger.LogError($"Failed to create user. Errors: {string.Join(", ", errors)}");
                        message = $"Failed to create user. Errors: {string.Join(", ", errors)}";
                        return AuthResFailed.CreateFailed(message);
                    }

                    // Add the user to the role
                    var addUserToRoleResult = await _userManager.AddToRoleAsync(user, Roles.User);
                    if (!addUserToRoleResult.Succeeded)
                    {
                        var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                        _logger.LogError($"Failed to add role to the user. Errors: {string.Join(", ", errors)}");
                        message= $"Failed to add role to the user. Errors: {string.Join(", ", errors)}";
                        return AuthResFailed.CreateFailed(message);
                    }

                    List<Claim> authClaims = new()
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.NameIdentifier, user.Id),
                            new Claim("purpose", "Email_Verification")//Email_Verification
                        };

                    var userRoles = await _userManager.GetRolesAsync(user);
                    foreach (var userRole in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                    }

                    var token = _tokenService.GenerateAccessToken(authClaims, true);

                    string link = _configuration["SwiftLineBaseUrl"] + "VerifyToken?token=" + token; //come back to this

                    string expirationTime = "1 hour";
                    await emailsDeliveryRepo.LogEmail(
                        username: user.UserName,
                        email: user.Email,              
                        subject: "Verify Your Email Address",
                        link: link,
                        type: EmailTypeEnum.Verify_Email,
                        ""
                        
                        );
                   
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    //Almost done🎉! A welcome mail has been sent to your email address. Kindly follow the instructions. Didn't get it in your inbox? Please check your spam folder or contact the support team. Thanks!
                    return new AuthRes(true,
                        "Almost done🎉! An email has been sent to your email address. Kindly follow the instructions. Didn't get it in your inbox? Please check your spam folder or contact the support team. Thanks!",
                        "", "", "", "", "", "SignUp");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Signup transaction failed: {ex.Message}");
                    return AuthResFailed.CreateFailed("Something went wrong. If this error persists please contact the support team.");
                }
            }

        }


        public async Task<AuthRes> Login(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            bool isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            string message = "";

            if (user is not null && string.IsNullOrEmpty(user.PasswordHash)) 
            {
                message = "No password created. You must have signed in with google. please sign in with google to continue or create a new account with a different email address.";
                return AuthResFailed.CreateFailed(message);
            }

            if (user is null || !isValidPassword)
            {
                message = "Invalid user name or password.";
                return AuthResFailed.CreateFailed(message);
            }

            if (!user.EmailConfirmed)
            {
                message = "Email address not verified, please check your email for the verification link and follow the instructions.";
                return AuthResFailed.CreateFailed(message);
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

            return new AuthRes(true, "Login Successful", token, refreshToken, user.Id, user.Email, user.UserName, "");

        }

        public async Task<AuthRes> RefreshToken(TokenModel tokenModel)
        {

            var principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            var username = principal.Identity.Name;
            string message = "";

            var tokenInfo = _context.TokenInfos.SingleOrDefault(u => u.Username == username);
            if (tokenInfo == null
                || tokenInfo.RefreshToken != tokenModel.RefreshToken
                || tokenInfo.ExpiredAt <= DateTime.UtcNow)
            {
                message = "Invalid or Expired token.";   
                return AuthResFailed.CreateFailed(message);
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            tokenInfo.RefreshToken = newRefreshToken; // rotating the refresh token
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByNameAsync(username);

            return new AuthRes(true, "Refresh token updated.", newAccessToken, newRefreshToken, user.Id, user.Email, user.UserName, "");

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

       
        public async Task<AuthRes> VerifyToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                string userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = _context.SwiftLineUsers.Find(userId);
                string message = "";
                if (user is null)
                {
                    message = "user not found with the provided token.";
                    _logger.LogError(message);
                    return AuthResFailed.CreateFailed(message);
                }
                user.EmailConfirmed = true;
                await emailsDeliveryRepo.LogEmail(
                        email: user.Email,
                        username: user.UserName,
                        subject: "Welcome to theswiftline 🤗",
                        link: _configuration["SwiftLineBaseUrl"],
                        type: EmailTypeEnum.Welcome
                        );
                 _context.SaveChanges();
                //not sending refresh token here.
                return new AuthRes(true, "Token Validated", token, "",
                    user.Id,
                    user.Email,
                    user.UserName,
                principal.FindFirst("purpose")?.Value);

            }
            catch (Exception ex)
            {
                //log exception
                _logger.LogError(ex, "Token validation failed: {Message}", ex.Message);
                return AuthResFailed.CreateFailed(ex.Message);
            }
        }  

        public async Task<string> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal)
        {
            //if (claimsPrincipal == null)
            //{
            //    throw new ExternalLoginProviderException("Google", "ClaimsPrincipal is null");
            //}

            bool isFirstSignIn = false;
            var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
            var fullName = claimsPrincipal.FindFirstValue(ClaimTypes.Name); 

            if (email == null)
            {
                throw new Exception();//ExternalLoginProviderException("Google", "Email is null");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                isFirstSignIn = true;
                SwiftLineUser newUser = new()
                {
                    Email = email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = await GetUniqueUsername(email),
                    EmailConfirmed = true,
                    FullName = fullName,
                };

                var result = await _userManager.CreateAsync(newUser);

                if (!result.Succeeded)
                {
                    //throw new ExternalLoginProviderException("Google",
                    //    $"Unable to create user: {string.Join(", ",
                    //        result.Errors.Select(x => x.Description))}");
                    throw new Exception();
                }
                
                user = newUser;
            }
            // Add the user to the role
            if (!await _userManager.IsInRoleAsync(user, Roles.User)) 
            {
                var addUserToRoleResult = await _userManager.AddToRoleAsync(user, Roles.User);
                if (!addUserToRoleResult.Succeeded)
                {
                    var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                    _logger.LogError($"Failed to add role to the user. Errors: {string.Join(", ", errors)}");
                    string message = $"Failed to add role to the user. Errors: {string.Join(", ", errors)}";
                    _logger.LogInformation(message);
                    return "";
                }
            }
           
            

            var info = new UserLoginInfo(
                "Google",
                claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                "Google");

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

            var loginResult = await _userManager.AddLoginAsync(user, info);

            var token = _tokenService.GenerateAccessToken(authClaims);
            //save refreshToken with exp date in the database
            var tokenInfo = _context.TokenInfos.
                        FirstOrDefault(a => a.Username == user.UserName);

            string refreshToken = _tokenService.GenerateRefreshToken();

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

            if (isFirstSignIn) 
            {
                await emailsDeliveryRepo.LogEmail(
                       email: user.Email,
                       username: user.UserName,
                       subject: "Welcome to theswiftline 🤗",
                       link: _configuration["SwiftLineBaseUrl"],
                       type: EmailTypeEnum.Welcome
                       );
            }

            //generate and capture authCode
            string authCode = GenerateRandomAlphanumericString() + DateTime.Now.Millisecond.ToString();

            var authCodeEntity = new AuthCodeData
            {
                Id = authCode,
                AccessToken = token,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Username = user.UserName,
                IsValid = true
            };

            await _context.AddAsync(authCodeEntity);
            await _context.SaveChangesAsync();

            return authCode;


        }

        public async Task<TurnstileResponse> VerifyTurnstile(TurnstileModel request)
        {
            using var client = new HttpClient();
            string cloudfare_verify_url = _configuration["Cloudfare:VerifyTurnsTileTokenUrl"];
            string cloudfare_secret_key = _configuration["Cloudfare:VerifyTurnsTileTokenSecret"];
            var values = new Dictionary<string, string>
            {
                { "secret", cloudfare_secret_key },
                { "response", request.TurnstileToken }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(cloudfare_verify_url, content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TurnstileResponse>(json);
        }

        public async Task<AnonymousUserAuthRes> CreateAnonymousUser()
        {

            // Check if any users exist to prevent duplicate seeding
            // Generate a unique username for the anonymous user
            int randNum = new Random().Next(0, 10000);
            var name = "Anonymous_" + randNum;
            int counter = 1;

            while (await _userManager.FindByNameAsync(name) is not null)
            {          
                randNum+=counter;
                name = "Anonymous_" + randNum;
                counter++;
            }
            var user = new SwiftLineUser
            {
                Name = "Anonymous",
                UserName = name,
                Email = $"{name}@gmail.com",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };


            if (!(await _roleManager.RoleExistsAsync(Roles.Anonymous)))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(Roles.Anonymous));
                if (!roleResult.Succeeded)
                {
                    return anonymousErrorInfo(roleResult,"Anonymous");
                }
            }

            // Attempt to create the anonymous user
            var createUserResult = await _userManager.CreateAsync(user, "Anonymous@123");
            if (!createUserResult.Succeeded)
            {
                return anonymousErrorInfo(createUserResult, "user");
            }

            // Assign the Anonymous role to the user
            var addUserToRoleResult = await _userManager.AddToRoleAsync(user, Roles.Anonymous);
            if (!addUserToRoleResult.Succeeded)
            {
                return anonymousErrorInfo(addUserToRoleResult, "role");
            }

            // Generate authentication claims and access token
            var authClaims = new List<Claim>
            {
               new Claim(ClaimTypes.Name, user.UserName),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var token = _tokenService.GenerateAccessToken(authClaims);
            await _context.SaveChangesAsync();

            return new AnonymousUserAuthRes(true, "Created Anonymous User Account",token, user);
        }

        public Task<List<SwiftLineUser>> GetExpiredAccounts()
        {
            var expiredAccounts = _context.SwiftLineUsers
                .Where(x => x.Name == "Anonymous" && x.DateCreated != default && x.DateCreated.AddDays(1) < DateTime.UtcNow.AddHours(1))
                .ToListAsync();

           return expiredAccounts;
        }

        public async Task DeleteExpiredAccount(SwiftLineUser user)
        {   
            await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();
            
        }
        private static string GenerateRandomAlphanumericString(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }
        private static string GenerateUsernameFromEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            // Split the email into local part and domain
            string[] parts = email.Split('@');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }

            string localPart = parts[0];

            // Remove any non-alphanumeric characters
            string cleanUsername = Regex.Replace(localPart, @"[^a-zA-Z0-9]", "");

            // If the cleaned username is too short, add some characters from the domain
            if (cleanUsername.Length < 4)
            {
                string domainPart = parts[1].Split('.')[0];
                cleanUsername += domainPart.Substring(0, Math.Min(4, domainPart.Length));
            }

            // Ensure the username is between 4 and 20 characters
            cleanUsername = cleanUsername.Length > 20
                ? cleanUsername[..20]
                : cleanUsername;

            // If the username is still too short, add numbers
            while (cleanUsername.Length < 4)
            {
                cleanUsername += new Random().Next(0, 9);
            }

            return cleanUsername.ToLower();
        }

        private async Task<string> GetUniqueUsername(string email)
        {
            string baseUsername = GenerateUsernameFromEmail(email);
            string uniqueUsername = baseUsername;
            int counter = 1;

            while (await _userManager.FindByNameAsync(uniqueUsername) is not null)
            {
                uniqueUsername = $"{baseUsername}{counter}";
                counter++;
            }

            return uniqueUsername;
        }
        private AnonymousUserAuthRes anonymousErrorInfo(IdentityResult result, string type)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            string message = type == "user" ? "Failed to create anonymous user." : "Failed to assign Anonymous role to user.";
            _logger.LogError($"Failed to {message}. Errors: {errors}");
            return new AnonymousUserAuthRes(false, $"{message}. Errors: {errors}", "", null);
        }
        public AuthRes GetAuthData(string authCode)
        {
            try
            {
                AuthCodeData record = _context.AuthCodeData.FirstOrDefault(x => x.Id == authCode && x.IsValid);
                if (record is not null) 
                {
                    record.IsValid = false;
                    _context.SaveChanges();

                    return new AuthRes(
                        true,
                        "Auth Data retrieved",
                        record.AccessToken,
                        record.RefreshToken,
                        record.UserId,
                        "",
                        record.Username
                        );
                }

                return new AuthRes(
                        false,
                        "Auth Data expired",
                       "",
                        "",
                        "",
                        "",
                        ""
                        );
                
            }
            catch (Exception ex)
            {

                throw ex;
            }
            

        }
       

       
    }


}
