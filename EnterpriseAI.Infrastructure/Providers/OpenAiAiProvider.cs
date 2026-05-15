using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Domain.Options;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Infrastructure.Providers
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
            await Task.Delay(500);
            return "[MOCK] ChatGPT: The system has been configured successfully based on the knowledge base.";
        }

        public IAsyncEnumerable<string> GetAnswerStreamAsync(string systemInstruction, string contextData, string userPrompt)
        {
            throw new NotImplementedException("Streaming is currently only implemented for Gemini in this repository.");
        }
    }
}

