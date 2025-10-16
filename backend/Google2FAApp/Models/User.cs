namespace Google2FAApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // plain for demo
        public string? TwoFASecret { get; set; } // store user secret key
        public bool Is2FAEnabled { get; set; } = false;
    }
}
