using Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public record AuthRes(bool status, string message, string AccessToken, string RefreshToken, string userId, string email, string username, string? purpose = "Login", bool isNewUser=false);

    public record AnonymousUserAuthRes(bool status, string message, string AccessToken, SwiftLineUser user);

    public static class AuthResFailed
    {
        public static AuthRes CreateFailed(string message)
   => new AuthRes(false, message, "", "", "", "", "");
    }
} 

