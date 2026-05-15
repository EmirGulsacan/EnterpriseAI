using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Domain.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Infrastructure.Providers.Gemini
{
    public class GeminiVisionProvider : IVisionProvider
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _geminiOptions;
        private readonly AiOptions _aiOptions;

        public GeminiVisionProvider(HttpClient httpClient, IOptions<GeminiOptions> geminiOptions, IOptions<AiOptions> aiOptions)
        {
            _httpClient = httpClient;
            _geminiOptions = geminiOptions.Value;
            _aiOptions = aiOptions.Value;
        }

        public async Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiOptions.ChatModel}:generateContent?key={_geminiOptions.ApiKey}";

            string base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new { contents = new[] { new { parts = new object[] { new { text = _aiOptions.VisionPrompt }, new { inline_data = new { mime_type = mimeType, data = base64Image } } } } } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return "Screenshot could not be analyzed.";

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;
        }
    }
}

