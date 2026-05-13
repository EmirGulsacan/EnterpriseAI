namespace EnterpriseAI.Shared.Options
{
    public class ClaudeOptions
    {
        public const string SectionName = "AiSettings:Claude";

        public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "claude-3-opus-20240229";
    }
}
