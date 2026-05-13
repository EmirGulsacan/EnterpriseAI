using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Shared.Providers.Gemini
{
    public class GeminiAiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public GeminiAiProvider(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetAnswerAsync(string systemInstruction, string contextData, string userPrompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.ChatModel}:generateContent?key={_options.ApiKey}";

            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[] { new { parts = new[] { new { text = $"CONTEXT: {contextData}\nQUESTION: {userPrompt}" } } } }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return "Gemini API Error: " + await response.Content.ReadAsStringAsync();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;
        }
    }
}