namespace Google2FA.Api.Models
{
    public class SetupResponse
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
    }
}
