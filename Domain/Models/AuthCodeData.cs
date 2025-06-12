
namespace Domain.Models
{
    public class AuthCodeData
    {
        public string Id { get; set; }

        public bool IsValid { get; set; }

        public string UserId { get; set; }

        public string Username { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
