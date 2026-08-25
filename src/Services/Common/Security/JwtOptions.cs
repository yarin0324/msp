namespace Common.Security
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";
        public string Issuer { get; set; } = "MSP.Identity";
        public string Audience { get; set; } = "MSP.Clients";
        public string SecretKey { get; set; } = "MSP_Microservices_Super_Secret_Key_2026_MinLength32Chars!";
        public int ExpirationMinutes { get; set; } = 120;
    }
}
