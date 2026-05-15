namespace EnterpriseAI.Domain.Options
{
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        public string ImageStoragePath { get; set; } = "Data/Images";
        public string ApiBaseUrl { get; set; } = "http://localhost:5102";
    }
}

