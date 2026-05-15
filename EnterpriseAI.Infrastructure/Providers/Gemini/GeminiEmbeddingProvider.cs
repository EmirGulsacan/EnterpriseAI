using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Domain.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Infrastructure.Providers.Gemini
{
    public class GeminiEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public GeminiEmbeddingProvider(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.EmbeddingModel}:embedContent?key={_options.ApiKey}";

            var requestBody = new { model = $"models/{_options.EmbeddingModel}", content = new { parts = new[] { new { text = text } } } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return Array.Empty<float>();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            var values = doc.RootElement.GetProperty("embedding").GetProperty("values");

            var result = new float[values.GetArrayLength()];
            for (int i = 0; i < result.Length; i++) result[i] = values[i].GetSingle();

            return result;
        }
    }
}

