using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Models;
using EnterpriseAI.Shared.Options;
using LiteDB;
using Microsoft.Extensions.Options;

using EnterpriseAI.Shared.Utils;

namespace EnterpriseAI.Api.Services
{
    public class KnowledgeBaseService
    {
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly string _dbPath;
        private readonly SearchOptions _searchOptions;
        private readonly DatabaseOptions _dbOptions;

        public KnowledgeBaseService(
            IEmbeddingProvider embeddingProvider, 
            IOptions<DatabaseOptions> dbOptions,
            IOptions<SearchOptions> searchOptions)
        {
            _embeddingProvider = embeddingProvider;
            _dbOptions = dbOptions.Value;
            _dbPath = PathHelper.GetAbsolutePathRelativeToSolution(_dbOptions.LiteDbPath);
            _searchOptions = searchOptions.Value;
        }

        public async Task<string> SearchHybridAsync(string userPrompt)
        {
            var queryVector = await _embeddingProvider.GenerateEmbeddingAsync(userPrompt);

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<DocumentChunk>("KnowledgeBase");

            var results = collection.FindAll()
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Similarity = CalculateCosineSimilarity(queryVector, chunk.Embedding)
                })
                .Where(x => x.Similarity >= _searchOptions.MinSimilarityScore)
                .OrderByDescending(x => x.Similarity)
                .Take(_searchOptions.TopK)
                .Select(x => $"[Sayfa: {x.Chunk.PageNumber}] {x.Chunk.Content.Replace("/images/", _dbOptions.ApiBaseUrl.TrimEnd('/') + "/images/")}")
                .ToList();

            return string.Join("\n\n---\n\n", results);
        }

        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null || vector1.Length != vector2.Length) return 0;

            float dotProduct = 0, magnitude1 = 0, magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            if (magnitude1 == 0 || magnitude2 == 0) return 0;
            return dotProduct / (MathF.Sqrt(magnitude1) * MathF.Sqrt(magnitude2));
        }
    }
}