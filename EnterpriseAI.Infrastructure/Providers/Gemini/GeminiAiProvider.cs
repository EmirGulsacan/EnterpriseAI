using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Domain.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EnterpriseAI.Infrastructure.Providers.Gemini
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

        public async IAsyncEnumerable<string> GetAnswerStreamAsync(string systemInstruction, string contextData, string userPrompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.ChatModel}:streamGenerateContent?alt=sse&key={_options.ApiKey}";

            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[] { new { parts = new[] { new { text = $"CONTEXT: {contextData}\nQUESTION: {userPrompt}" } } } }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                yield return "Gemini API Error: " + await response.Content.ReadAsStringAsync();
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6).Trim();
                    if (data == "[DONE]") break;

                    string? chunkText = null;
                    try 
                    {
                        using var doc = JsonDocument.Parse(data);
                        if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                        {
                            var candidate = candidates[0];
                            if (candidate.TryGetProperty("content", out var content) && 
                                content.TryGetProperty("parts", out var parts) && 
                                parts.GetArrayLength() > 0)
                            {
                                if (parts[0].TryGetProperty("text", out var textElement))
                                {
                                    chunkText = textElement.GetString();
                                }
                            }
                        }
                    }
                    catch (JsonException) { }

                    if (chunkText != null)
                    {
                        yield return chunkText;
                    }
                }
            }
        }
    }
}

