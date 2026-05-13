namespace EnterpriseAI.Shared.Options
{
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        public string LiteDbPath { get; set; } = "Data/EnterpriseRAG.db";
        public string ImageStoragePath { get; set; } = "Data/Images";
        public string ApiBaseUrl { get; set; } = "http://localhost:5102";
    }
}
