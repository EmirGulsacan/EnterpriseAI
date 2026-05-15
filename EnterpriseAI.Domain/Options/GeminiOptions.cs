namespace EnterpriseAI.Domain.Options
{
    public class GeminiOptions
    {
        public const string SectionName = "AiSettings:Gemini";

        public string ApiKey { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = "gemini-embedding-001";
        public string ChatModel { get; set; } = "gemini-2.0-flash";
    }
}

