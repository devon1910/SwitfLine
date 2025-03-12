using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace Domain.AttributeValidator
{
    public class JwtTokenAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var token = value as string;

            if (string.IsNullOrEmpty(token))
            {
                return new ValidationResult("JWT token cannot be null or empty.");
            }

            // JWT tokens have three parts separated by dots
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return new ValidationResult("Invalid JWT token format. Expected three parts separated by dots.");
            }

            // Check if each part can be decoded from base64
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    return new ValidationResult("The provided string is not a valid JWT token.");
                }

                // Optional: You can also try to read the token to validate its structure further
                var jwtToken = handler.ReadJwtToken(token);

                // At this point, you know the token is structurally valid
                return ValidationResult.Success;
            }
            catch (Exception ex)
            {
                return new ValidationResult($"JWT token validation failed: {ex.Message}");
            }
        }
    }
}
