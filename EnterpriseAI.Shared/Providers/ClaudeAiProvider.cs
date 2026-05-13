using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Options;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Shared.Providers
{
    public class ClaudeAiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ClaudeOptions _options;

        public ClaudeAiProvider(HttpClient httpClient, IOptions<ClaudeOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetAnswerAsync(string systemInstruction, string contextData, string userPrompt)
        {
            // TODO: Implement real Claude API call using _options.BaseUrl, _options.ApiKey, _options.ChatModel
            await Task.Delay(500);
            return "[MOCK] Claude 3: Knowledge base analysis completed for the given context.";
        }
    }
}