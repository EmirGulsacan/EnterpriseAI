using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Domain.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Infrastructure.Providers.LiteLlm
{
    public class LiteLlmEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly LiteLlmOptions _options;

        public LiteLlmEmbeddingProvider(HttpClient httpClient, IOptions<LiteLlmOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/embeddings";
            var requestBody = new { model = _options.EmbeddingModel, input = text };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[LiteLLM EMBEDDING ERROR] {response.StatusCode}");
                return Array.Empty<float>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            var embeddingElement = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

            var result = new float[embeddingElement.GetArrayLength()];
            for (int i = 0; i < result.Length; i++) result[i] = embeddingElement[i].GetSingle();

            return result;
        }
    }
}

