namespace Google2FA.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // hashed in real app
        public string? SecretKey { get; set; } // for 2FA
        public bool Is2FAEnabled { get; set; } = false;
    }
}
