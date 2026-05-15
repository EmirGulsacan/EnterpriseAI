using System.Threading.Tasks;

namespace EnterpriseAI.Application.Interfaces
{
    public interface IEmbeddingProvider
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
    }

    public interface IVisionProvider
    {
        Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType);
    }

    public interface IAiProvider
    {
        Task<string> GetAnswerAsync(string systemInstruction, string contextData, string userPrompt);
        IAsyncEnumerable<string> GetAnswerStreamAsync(string systemInstruction, string contextData, string userPrompt);
    }
}

