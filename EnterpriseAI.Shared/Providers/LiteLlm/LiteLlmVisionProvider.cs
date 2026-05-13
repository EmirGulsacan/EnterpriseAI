using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Shared.Providers.LiteLlm
{
    public class LiteLlmVisionProvider : IVisionProvider
    {
        private readonly HttpClient _httpClient;
        private readonly LiteLlmOptions _liteLlmOptions;
        private readonly AiOptions _aiOptions;

        public LiteLlmVisionProvider(HttpClient httpClient, IOptions<LiteLlmOptions> liteLlmOptions, IOptions<AiOptions> aiOptions)
        {
            _httpClient = httpClient;
            _liteLlmOptions = liteLlmOptions.Value;
            _aiOptions = aiOptions.Value;
        }

        public async Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType)
        {
            var url = $"{_liteLlmOptions.BaseUrl.TrimEnd('/')}/chat/completions";
            string base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new
            {
                model = _liteLlmOptions.ChatModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = _aiOptions.VisionPrompt },
                            new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Image}" } }
                        }
                    }
                },
                max_tokens = 500
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _liteLlmOptions.ApiKey);

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return "Screenshot could not be analyzed.";

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
        }
    }
}