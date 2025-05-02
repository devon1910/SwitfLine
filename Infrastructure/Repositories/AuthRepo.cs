using Domain.Constants;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Infrastructure.RetryLogic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using System;
using System.Collections.Generic;
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
                                IFluentEmail _fluentEmail,
                                ILogger<AuthRepo> _logger,
                                IConfiguration _configuration) : IAuthRepo
    {

        public async Task<AuthRes> Signup(SignupModel model)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        return new AuthRes(
                            false,
                            existingUser.EmailConfirmed
                                ? "User already exists"
                                : "User already exists but email is not verified. Please check your email for the verification link and follow the instructions.",
                            "", "", "", "", false, "");
                    }

                    // Create User role if it doesn't exist
                    if (!(await _roleManager.RoleExistsAsync(Roles.User)))
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole(Roles.User));
                        if (!roleResult.Succeeded)
                        {
                            var roleErrors = roleResult.Errors.Select(e => e.Description);
                            _logger.LogError($"Failed to create user role. Errors: {string.Join(", ", roleErrors)}");
                            return new AuthRes(false, $"Failed to create user role. Errors: {string.Join(", ", roleErrors)}", "", "", "", "", false, "");
                        }
                    }

                    // Create the new user
                    SwiftLineUser user = new()
                    {
                        Email = model.Email,
                        FullName = model.FullName,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        UserName = await GetUniqueUsername(model.Email),
                        EmailConfirmed = true,// just for now, veification later on.            
                    };

                    var createUserResult = await _userManager.CreateAsync(user, model.Password);
                    if (!createUserResult.Succeeded)
                    {
                        var errors = createUserResult.Errors.Select(e => e.Description);
                        _logger.LogError($"Failed to create user. Errors: {string.Join(", ", errors)}");
                        return new AuthRes(false, $"Failed to create user. Errors: {string.Join(", ", errors)}", "", "", "", "", false, "");
                    }

                    // Add the user to the role
                    var addUserToRoleResult = await _userManager.AddToRoleAsync(user, Roles.User);
                    if (!addUserToRoleResult.Succeeded)
                    {
                        var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                        _logger.LogError($"Failed to add role to the user. Errors: {string.Join(", ", errors)}");
                        return new AuthRes(false, $"Failed to add role to the user. Errors: {string.Join(", ", errors)}", "", "", "", "", false, "");
                    }

                    // Build authentication claims including a claim to signal email verification purpose
                    List<Claim> authClaims = new()
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.NameIdentifier, user.Id),
                            new Claim("purpose", "SignUp")//Email_Verification
                        };

                    var userRoles = await _userManager.GetRolesAsync(user);
                    foreach (var userRole in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                    }

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

                    //// Send Email Verification with retry logic
                    //bool isMailSent = await RetryPolicy.ExecuteAsync(async () =>
                    //   await SendEmailVerifyLink(user.Email, token, user.UserName),
                    //   maxRetryCount: 3,
                    //   delayBetweenRetries: TimeSpan.FromSeconds(3));              

                    //// If sending the email fails, throw to trigger rollback.
                    //if (!isMailSent)
                    //{
                    //    throw new Exception("User account created but unable to send out verification email at the moment.");
                    //}
                    // Send Email Verification with retry logic
                    bool isMailSent = await RetryPolicy.ExecuteAsync(async () =>
                       await SendWelcomeMail(user.Email, user.UserName),
                       maxRetryCount: 3,
                       delayBetweenRetries: TimeSpan.FromSeconds(3));

                    // If sending the email fails, throw to trigger rollback.
                    if (!isMailSent)
                    {
                        throw new Exception("User account created but Welcome Mail wasn't sent.");
                    }

                    // Commit transaction if everything succeeded
                    await transaction.CommitAsync();

                    //Almost done🎉! A welcome mail has been sent to your email address. Kindly follow the instructions. Didn't get it in your inbox? Please check your spam folder or contact the support team. Thanks!
                    return new AuthRes(true,
                        "Welcome!",
                        token, refreshToken, user.Id, user.Email, user.IsInQueue, user.UserName, "SignUp");
                }
                catch (Exception ex)
                {
                    // Roll back all changes if any error occurs
                    await transaction.RollbackAsync();
                    _logger.LogError($"Signup transaction failed: {ex.Message}");
                    return new AuthRes(false, ex.Message, "", "", "", "", false, "");
                }
            }

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


        public async Task<AuthRes> Login(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            bool isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (user is null || !isValidPassword)
            {
                return new AuthRes(false, "Invalid user name or password.", "", "", "", "", false, "");
            }

            if (!user.EmailConfirmed)
            {
                return new AuthRes(false, "Email address not verified, please check your email for the verification link and follow the instructions.", "", "", "", "", false, "");
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

            return new AuthRes(true, "Login Successful", token, refreshToken, user.Id, user.Email, user.IsInQueue, user.UserName, "");

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
                return new AuthRes(false, "Invalid refresh token. Please login again.", "", "", "", "", false, "");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            tokenInfo.RefreshToken = newRefreshToken; // rotating the refresh token
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByNameAsync(username);

            return new AuthRes(true, "Refresh token updated.", newAccessToken, newRefreshToken, user.Id, user.Email, user.IsInQueue, user.UserName, "");

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

        public async Task<bool> SendEmailVerifyLink(string RecipientEmail, string token, string username)
        {
            string htmlTemplate = GetEmailTemplate();
            string link = _configuration["SwiftLineBaseUrl"] + token; //come back to this
            var email = await _fluentEmail
                .To(RecipientEmail)
                .Subject($"Welcome to Swiftline ⏭ - Verify Your Email Address")
                .Body(htmlTemplate
                .Replace("{{UserName}}", username)
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

        private async Task<bool> SendWelcomeMail(string RecipientEmail, string username)
        {

            string htmlTemplate = GetWelcomeEmailTemplate();
            string link = _configuration["SwiftLineBaseUrlForReminder"]; //come back to this
            var email = await _fluentEmail
                .To(RecipientEmail)
                .Subject($"Welcome to Swiftline ⏭")
                .Body(htmlTemplate
                .Replace("{{UserName}}", username)
                .Replace("{{SwiftlineUrl}}", link), true)
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
                var user = _context.SwiftLineUsers.Find(userId);

                if (user is null)
                {
                    return new AuthRes(false, "user not found with the provided token.", "", "", "", "", false, "");
                }
                user.EmailConfirmed = true;
                _context.SaveChanges();
                return new AuthRes(true, "Token Validated", "", "",
                    user.Id,
                    user.Email,
                    user.IsInQueue,
                    user.UserName,
                principal.FindFirst("purpose")?.Value);

            }
            catch (Exception ex)
            {
                //log exception
                return new AuthRes(false, "Sorry, Something went wrong. Please sign up or contact the support team if this error persists.", "", "", "", "", false, "");
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
                             .center {
                                display: block;
                                margin-left: auto;
                                margin-right: auto;
                                width: 30%;
                            }
                        </style>
                    </head>
                    <body>
                       
    
                        <div class=""container"">
                            
                            <div>
                                 <img src = ""https://res.cloudinary.com/dddabj5ub/image/upload/v1741908218/swifline_logo_cpsacv.webp"" alt=""Swiftline"" class=""center"">
                            </div>
                            <p>Hello {{UserName}},</p>
        
                            <p>Thank you for Signing up with Swiftline! We're excited to have you on board. To get started with your account, please verify your email address by clicking the button below:</p>
        
                            <div style=""text-align: center;"">
                                <a href=""{{VerificationLink}}"" class=""button"">Verify Email Address</a>
                            </div>
        
                            <p class=""expiry-note"">⏱️ This verification link expires in 1 hour</p>
        
                            <p>If this mail came in your spam folder, the button above wouldn't work. Click on the ""Report as not a spam button"" above to move the mail to your inbox and try to click on the button.
                               OR 
                            you can copy and paste the following link into your browser:</p>
        
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


        }

        private string GetWelcomeEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to SwiftLine Queue Management ⏩</title>
    <style>
          @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

        body {
             margin: 0;
            padding: 0;
            font-family: 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            background-color: #f5f5f5;
        }

        .container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background-color: #8BA888; /* Sage green */
            padding: 30px;
            text-align: center;
            text-color: #ffffff
        }
        .logo {
            font-size: 28px;
            font-weight: bold;
            color: #ffffff;
            letter-spacing: 1px;
        }
        .tagline {
            color: #ffffff;
            margin-top: 10px;
            font-style: italic;
        }
        .content {
            padding: 30px;
            line-height: 1.6;
        }
        h1 {
            color: #333333;
            margin-top: 0;
        }
        h2 {
            color: #8BA888;
            margin-top: 30px;
        }
        .button {
            display: inline-block;
            background-color: #8BA888; /* Sage green */
            color: #ffffff !important;
            text-decoration: none;
            padding: 12px 24px;
            border-radius: 4px;
            font-weight: bold;
            margin: 20px 0;
        }
        .button:hover {
            background-color: #7A9977;
        }
        .features {
            margin: 30px 0;
        }
        .feature {
            display: flex;
            align-items: flex-start;
            margin-bottom: 20px;
        }
        .feature-icon {
            min-width: 30px;
            height: 30px;
            background-color: #8BA888;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-right: 15px;
            color: white;
            font-weight: bold;
        }
        .feature-content {
            flex: 1;
        }
        .feature-title {
            font-weight: bold;
            margin-bottom: 5px;
        }
        .feature-description {
            color: #555555;
        }
        .steps {
            background-color: #f5f5f5;
            padding: 20px;
            border-radius: 4px;
            margin: 30px 0;
        }
        .step {
            margin-bottom: 15px;
        }
        .step-number {
            display: inline-block;
            background-color: #8BA888;
            color: white;
            width: 24px;
            height: 24px;
            text-align: center;
            border-radius: 50%;
            margin-right: 10px;
        }
        .footer {
            background-color: #f5f5f5;
            padding: 20px;
            text-align: center;
            color: #666666;
            font-size: 12px;
        }
        .social {
            margin: 15px 0;
        }
        .social a {
            display: inline-block;
            margin: 0 8px;
            color: #8BA888;
            text-decoration: none;
        }
         .center {
            display: block;
            margin-left: auto;
            margin-right: auto;
            width: 30%;
        }
    </style>
</head>
<body>
    <div class=""container"">
       
        <div class=""content"">
            <h1>Welcome to a Smarter Way to Queue!</h1>
            
            <p>Hi {{UserName}},</p>
            
            <p>Thank you for choosing Swiftline! We're excited to have you join our community of businesses and organizations that are transforming how people wait in line. <br><br>
            Your account has been successfully created, and you're now ready to start managing queues more efficiently.</p>
            
            <a href=""{{SwiftlineUrl}}"" class=""button"">Set Up Your First Queue</a>
            
            <h2>Why Swiftline Makes Waiting Better</h2>
            
            <div class=""features"">
                <div class=""feature"">
                    <div>🔄</div>
                    <div class=""feature-content"">
                        <div class=""feature-title""> Join From Anywhere</div>
                        <div class=""feature-description"">Your customers can join queues remotely from their devices, eliminating the need to physically stand in line.</div>
                    </div>
                </div>
                
                <div class=""feature"">
                    <div>⏱️</div>
                    <div class=""feature-content"">
                        <div class=""feature-title""> Real-Time Updates</div>
                        <div class=""feature-description"">Automatic notifications keep everyone informed about queue status, an email reminder is sent when it's almost your turn and estimated wait times.</div>
                    </div>
                </div>
                
                <div class=""feature"">
                    <div>✓</div>
                    <div class=""feature-content"">
                        <div class=""feature-title""> Time Efficiency</div>
                        <div class=""feature-description"">Users can multitask and make better use of their time while waiting for their turn.</div>
                    </div>
                </div>
                
                <div class=""feature"">
                    <div>📊</div>
                    <div class=""feature-content"">
                        <div class=""feature-title""> Powerful Analytics</div>
                        <div class=""feature-description"">Gain insights into wait times, peak hours, and customer flow to optimize your operations.</div>
                    </div>
                </div>
            </div>
            
            <div class=""steps"">
                <h2>Get Started in 3 Simple Steps:</h2>
                
                <div class=""step"">
                    <span class=""step-number"">1</span>
                    <strong>Set up your event or service</strong> - Create your first queue and customize it to fit your needs
                </div>
                
                <div class=""step"">
                    <span class=""step-number"">2</span>
                    <strong>Share your queue link</strong> - Invite people to join your queue through email, SMS, or QR code
                </div>
                
                <div class=""step"">
                    <span class=""step-number"">3</span>
                    <strong>Start managing your queue</strong> - Use our dashboard to track and manage participants efficiently
                </div>
            </div>
            
            <p>Our support team is available 24/7 to help you get the most out of Swiftline. If you have any questions or need assistance setting up your queues, don't hesitate to reach out.</p>
            
            <p>Say goodbye to long lines and hello to happy customers!</p>
            
            <p>Best regards,<br>The Swiftline Team</p>
        </div>
        
        <div class=""footer"">
            <div class=""social"">
                <a href=""#"">Twitter</a> | 
                <a href=""#"">Facebook</a> | 
                <a href=""#"">Instagram</a> | 
                <a href=""#"">LinkedIn</a>
            </div>
            <p>© 2025 Swiftline Queue Management. All rights reserved.</p>
            <p>You're receiving this email because you signed up for Swiftline.<br>
            <a href=""#"" style=""color: #8BA888;"">Unsubscribe</a> | <a href=""#"" style=""color: #8BA888;"">Privacy Policy</a></p>
        </div>
    </div>
</body>
</html>";
        }

        public async Task<AuthRes> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal)
        {
            //if (claimsPrincipal == null)
            //{
            //    throw new ExternalLoginProviderException("Google", "ClaimsPrincipal is null");
            //}

            var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
            var fullName = claimsPrincipal.FindFirstValue(ClaimTypes.Name);

            if (email == null)
            {
                throw new Exception();//ExternalLoginProviderException("Google", "Email is null");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
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

            await _context.SaveChangesAsync();

            return new AuthRes(true, "Login Successful", token, refreshToken, user.Id, user.Email, user.IsInQueue, user.UserName);


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

        public async Task<AuthRes> CreateAnonymousUser()
        {

            // Check if any users exist to prevent duplicate seeding
            var name = "Anonymous_"+DateTime.UtcNow.AddHours(1).ToString().Replace("/","_").Replace(" ","_").Replace(":","_");
            var user = new SwiftLineUser
            {
                Name = "Anonymous",
                UserName = name,
                Email = name + "@gmail.com",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };


            // Attempt to create admin user
            var createUserResult = await _userManager
                  .CreateAsync(user: user, password: "Anonymous@123");

            // Validate user creation
            if (!createUserResult.Succeeded)
            {
                var errors = createUserResult.Errors.Select(e => e.Description);
                _logger.LogError(
                    $"Failed to create anonymous user. Errors: {string.Join(", ", errors)}"
                );
                return new AuthRes(false,
                      "Failed to create anonymous user!",
                      "", "", "", "", false, "Anonymous", "SignUp");
            }

            if (!(await _roleManager.RoleExistsAsync(Roles.Anonymous)))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(Roles.Anonymous));
                if (!roleResult.Succeeded)
                {
                    var roleErrors = roleResult.Errors.Select(e => e.Description);
                    _logger.LogError($"Failed to create user role. Errors: {string.Join(", ", roleErrors)}");
                    return new AuthRes(false, $"Failed to create user role. Errors: {string.Join(", ", roleErrors)}", "", "", "", "", false, "");
                }
            }

            // Add the user to the role
            var addUserToRoleResult = await _userManager.AddToRoleAsync(user, Roles.Anonymous);
            if (!addUserToRoleResult.Succeeded)
            {
                var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                _logger.LogError($"Failed to add role to the user. Errors: {string.Join(", ", errors)}");
                return new AuthRes(false, $"Failed to add role to the user. Errors: {string.Join(", ", errors)}", "", "", "", "", false, "");
            }

            List<Claim> authClaims = new()
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.NameIdentifier, user.Id),
                        };


            var token = _tokenService.GenerateAccessToken(authClaims);
            await _context.SaveChangesAsync();

            return new AuthRes(true,
                      "Welcome!",
                      token, "", user.Id, user.Email, user.IsInQueue, "Anonymous", "SignUp");



            //var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
            //var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();

            //var refreshTokenExpirationDateInUtc = DateTime.UtcNow.AddDays(7);

            //user.RefreshToken = refreshTokenValue;
            //user.RefreshTokenExpiresAtUtc = refreshTokenExpirationDateInUtc;

            //await _userManager.UpdateAsync(user);

            //_authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expirationDateInUtc);
            //_authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, refreshTokenExpirationDateInUtc);
        }

        public Task<List<SwiftLineUser>> GetExpiredAccounts()
        {
            var expiredAccounts = _context.SwiftLineUsers
                .Where(x => x.Name == "Anonymous" && x.DateCreated != default && x.DateCreated.AddDays(1) < DateTime.UtcNow.AddHours(1))
                .ToListAsync();

           return expiredAccounts;
        }

        public async Task<bool> DeleteExpiredAccount(SwiftLineUser user)
        {   
            await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();
        }
    }
    }
