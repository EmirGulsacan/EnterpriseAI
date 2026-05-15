namespace EnterpriseAI.Domain.Options
{
    public class OpenAiOptions
    {
        public const string SectionName = "AiSettings:OpenAI";

        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "gpt-4o";
    }
}

