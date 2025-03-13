using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Numerics;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Infrastructure.Repositories
{
    public class AuthRepo(UserManager<SwiftLineUser> _userManager,
                                RoleManager<IdentityRole> _roleManager,
                                SwiftLineDatabaseContext _context, 
                                ITokenRepo _tokenService,
                                IFluentEmail _fluentEmail,
                                ILogger<AuthRepo> _logger,
                                IConfiguration _configuration) : IAuthRepo
    {
 
        public async Task<AuthRes> Signup(SignupModel model)
        {
            var existingUser = await _userManager.FindByNameAsync(model.Email);
            if (existingUser != null)
            {
                return new AuthRes(false,
                    existingUser.IsEmailVerified 
                    ? "User already exists"
                    : "User already exists but email is not verified. Please check your email for the verification link and follow the instructions.", "","","","",false);
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

            List<Claim> authClaims = [
                   new (ClaimTypes.Name, user.UserName),
                        new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new (ClaimTypes.NameIdentifier, user.Id),
                        new ("purpose", "Email_Verification"),
                        // unique id for token
                        ];

            var userRoles = await _userManager.GetRolesAsync(user);

            // adding roles to the claims. So that we can get the user role from the token.
            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = _tokenService.GenerateAccessToken(authClaims);
            //Send Email Verification
            bool isMailSent= await SendEmailVerifyLink(user.Email, token); 
           
            return new(isMailSent ? true : false,
                isMailSent ? "Almost done🎉! a welcome mail has been sent to your email address, kindly follow the instructions. Didn't get it in your inbox? Please check your spam folder or contact the support team. Thanks!"
                : "User account created but unable to send out emails at the moment.",
                "", "", user.Id, user.Email,user.IsInQueue);

        }

        public async Task<AuthRes> Login(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            bool isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (user is null || !isValidPassword)
            {
                return new AuthRes(false, "Invalid user name or password.", "", "", "", "",false);
            }

            if (!user.IsEmailVerified)
            {
                return new AuthRes(false, "Email address not verified, please check your email for the verification link and follow the instructions.", "", "", "", "", false);
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

        public async Task<bool> SendEmailVerifyLink(string RecipientEmail, string token)
        {
            string htmlTemplate = GetEmailTemplate(); 
            string link =  _configuration["SwiftLineBaseUrl"] +token; //come back to this
            var email = await _fluentEmail
                .To(RecipientEmail)
                .Subject($"Welcome to Swiftline⚡ - Verify Your Email Address")
                .Body(htmlTemplate
                .Replace("{{UserName}}",RecipientEmail)
                .Replace("{{VerificationLink}}", link), true) 
                .SendAsync();
            _logger.LogInformation("Email Sent Successfully");
            if (!email.Successful)
            {
                _logger.LogError("Failed to send email: {Errors}",
                    string.Join(", ", email.ErrorMessages));
                throw new Exception("Failed to send welcome Email");
            }
            return true;
        }

        public AuthRes VerifyToken(string token)
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
                var user= _context.SwiftLineUsers.Find(userId);

                if (user is null)
                {
                    return new AuthRes(false, "Could not found user with the provided token.", "", "", "", "", false);
                }
                user.IsEmailVerified = true;
                _context.SaveChanges();
                return new AuthRes(true,"Token Validated","","", 
                    user.Id, 
                    user.Email,
                    user.IsInQueue,
                    principal.FindFirst("purpose")?.Value);
            }
            catch (Exception ex)
            {
                throw ex; // Invalid token
            }
        }

        private string GetEmailTemplate()
        {
            return @"
                  <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                      

                        <title>Welcome to Swiftline</title>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');
        
                            body {
                                font-family: 'Inter', sans-serif;
                                line-height: 1.6;
                                color: #333;
                                max-width: 600px;
                                margin: 0 auto;
                                padding: 20px;
                                background-color: #f9f9f9;
                            }
                            .logo {
                                text-align: center;
                                margin-bottom: 30px;
                            }
                            .logo img {
                                max-width: 180px;
                            }
                            .container {
                                background-color: white;
                                border-radius: 8px;
                                padding: 30px;
                                border: 1px solid #eaeaea;
                            }
                            h1 {
                                color: #6B8E6E; /* Sage green */
                                margin-top: 0;
                                font-weight: 600;
                            }
                            .button {
                                display: inline-block;
                                background-color: #6B8E6E; /* Sage green */
                                color: white;
                                text-decoration: none;
                                padding: 12px 30px;
                                border-radius: 4px;
                                font-weight: 500;
                                margin: 20px 0;
                            }
                            .expiry-note {
                                color: #6B8E6E; /* Sage green */
                                font-weight: 600;
                                font-size: 14px;
                                border: 1px solid #6B8E6E;
                                display: inline-block;
                                padding: 8px 15px;
                                border-radius: 4px;
                            }
                            .footer {
                                margin-top: 30px;
                                font-size: 12px;
                                color: #666;
                                text-align: center;
                            }
                            .link {
                                color: #6B8E6E; /* Sage green */
                                text-decoration: underline;
                            }
                        </style>
                    </head>
                    <body>
                       
    
                        <div class=""container"">
                           
        
                            <p>Hello {{UserName}},</p>
        
                            <p>Thank you for Signing up with Swiftline! We're excited to have you on board. To get started with your account, please verify your email address by clicking the button below:</p>
        
                            <div style=""text-align: center;"">
                                <a href=""{{VerificationLink}}"" class=""button"">Verify Email Address</a>
                            </div>
        
                            <p class=""expiry-note"">⏱️ This verification link expires in 1 hour</p>
        
                            <p>If the button above doesn't work, you can copy and paste the following link into your browser:</p>
        
                            <p style=""word-break: break-all; font-size: 14px; color: #666;"">{{VerificationLink}}</p>
        
                            <p>Swiftline is designed to help you manage your workflow efficiently and boost your productivity. Once your email is verified, you'll have full access to all features.</p>
                            
                            <p> If you have any questions or need assistance, please don't hesitate to contact our support team at <a href=""mailto:swiftline00@gmail.com"" class=""link"">swiftline00@gmail.com</a>.</p>

                            
                            <p>Best regards,<br>
                            The Swiftline Team</p>
                        </div>
    
                        <div class=""footer"">
                            <p>© 2025 Swiftline. All rights reserved.</p>
                            <p>Visit our website: <a href=""https://swiftline-olive.vercel.app"" class=""link"">https://swiftline-olive.vercel.app</a></p>
                            <p>If you didn't create this account, please ignore this email.</p>
                        </div>
                    </body>
                    </html>";

//              < div class=""logo"">
//                           <img style = ""style=width: 60px; margin-bottom: 1em; display: block; margin-left: auto; margin-right: auto;""
//src = ""https://media-hosting.imagekit.io//6f445e007cc74d9e/swifline_logo.webp?Expires=1836439304&Key-Pair-Id=K2ZIVPTIP2VGHC&Signature=vJ5r56sZuDVm1UFUr3qSMU7L~InBoubqGGRjK2QVMOLNP057l9gP3oho6wXYo0HwP2DjdYpJHWScz5ZYEkqCuzLmplHdZF3mHBWhVnNczB-C-5Ac4eLgBOw0KPt~ieI62GInXhVLyBF58MvOeoFhSrP6hM17EN307XAnUkelCR2XfCcYu746ItonZ3arrC3k7ZE1F6NjqrV7zcPf9X0OdcZd5vlq5mRUWklwAiChanubKtBtS3Iwu7gvzqjhQPID5B34eWRRHENaXfGZPQKRywvwR2svax53QJbVNIm3RLcR0691~L527KuqDRS1wa7HNVwJWbTi5WxNxrc16G-HSA__"" alt=""Swiftline Logo"">
//                        </div>
        }
    }
}
