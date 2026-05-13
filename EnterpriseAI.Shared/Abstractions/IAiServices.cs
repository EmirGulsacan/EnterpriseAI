using System.Threading.Tasks;

namespace EnterpriseAI.Shared.Abstractions
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
    }
}