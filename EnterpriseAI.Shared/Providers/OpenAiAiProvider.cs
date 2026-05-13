using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Options;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Shared.Providers
{
    public class OpenAiAiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;

        public OpenAiAiProvider(HttpClient httpClient, IOptions<OpenAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetAnswerAsync(string systemInstruction, string contextData, string userPrompt)
        {
            // TODO: Implement real OpenAI API call using _options.BaseUrl, _options.ApiKey, _options.ChatModel
            await Task.Delay(500);
            return "[MOCK] ChatGPT: The system has been configured successfully based on the knowledge base.";
        }
    }
}