using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Domain.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Infrastructure.Providers.LiteLlm
{
    public class LiteLlmAiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly LiteLlmOptions _options;

        public LiteLlmAiProvider(HttpClient httpClient, IOptions<LiteLlmOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetAnswerAsync(string systemInstruction, string contextData, string userPrompt)
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
            var requestBody = new
            {
                model = _options.ChatModel,
                messages = new[]
                {
                    new { role = "system", content = $"{systemInstruction}\nCONTEXT:\n{contextData}" },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return $"LiteLLM Chat Error: {await response.Content.ReadAsStringAsync()}";

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
        }

        public IAsyncEnumerable<string> GetAnswerStreamAsync(string systemInstruction, string contextData, string userPrompt)
        {
            throw new NotImplementedException("Streaming is currently only implemented for Gemini in this repository.");
        }
    }
}

