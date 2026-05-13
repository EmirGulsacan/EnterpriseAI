namespace EnterpriseAI.Shared.Options
{
    public class JwtOptions
    {
        public const string SectionName = "JwtSettings";

        public string Secret { get; set; } = string.Empty;
        public int ExpirationInHours { get; set; } = 1;
    }
}
