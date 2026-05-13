namespace EnterpriseAI.Shared.Options
{
    public class LiteLlmOptions
    {
        public const string SectionName = "AiSettings:LiteLLM";

        public string BaseUrl { get; set; } = "https://your-litellm-proxy.example.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "openai/your-model-name";
        public string EmbeddingModel { get; set; } = "openai/BAAI/bge-m3";
    }
}
