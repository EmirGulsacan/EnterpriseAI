using EnterpriseAI.Domain.Entities;
using EnterpriseAI.Domain.Options;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using EnterpriseAI.Domain.Utils;

namespace EnterpriseAI.Application.Services
{
    public class KnowledgeBaseService
    {
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IApplicationDbContext _dbContext;
        private readonly SearchOptions _searchOptions;
        private readonly DatabaseOptions _dbOptions;

        public KnowledgeBaseService(
            IApplicationDbContext dbContext,
            IEmbeddingProvider embeddingProvider, 
            IOptions<DatabaseOptions> dbOptions,
            IOptions<SearchOptions> searchOptions)
        {
            _dbContext = dbContext;
            _embeddingProvider = embeddingProvider;
            _dbOptions = dbOptions.Value;
            _searchOptions = searchOptions.Value;
        }

        public async Task<(string ContextString, List<ChunkReferenceDto> References)> SearchHybridAsync(AiRequestDto request)
        {
            var queryVectorArray = await _embeddingProvider.GenerateEmbeddingAsync(request.UserPrompt);
            var queryVector = new Pgvector.Vector(queryVectorArray);

            var query = _dbContext.Set<DocumentChunk>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.ApplicationCode))
            {
                query = query.Where(x => x.ApplicationCode == request.ApplicationCode);
            }

            if (!string.IsNullOrWhiteSpace(request.TargetDocument))
            {
                query = query.Where(x => x.SourceDocument == request.TargetDocument);
            }

            var dbResults = await query
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Distance = chunk.Embedding!.CosineDistance(queryVector)
                })
                .Where(x => (1 - x.Distance) >= _searchOptions.MinSimilarityScore)
                .OrderBy(x => x.Distance)
                .Take(_searchOptions.TopK)
                .ToListAsync();

            var results = dbResults.Select(x => 
            {
                string imagePath = string.Empty;
                if (x.Chunk.Metadata != null && x.Chunk.Metadata.RootElement.TryGetProperty("ImagePath", out var imagePathElement))
                {
                    imagePath = imagePathElement.GetString() ?? string.Empty;
                }

                return new ChunkReferenceDto
                {
                    SourceDocument = x.Chunk.SourceDocument,
                    PageNumber = x.Chunk.PageNumber,
                    ImagePath = imagePath,
                    ContentSnippet = x.Chunk.Content.Replace("/images/", _dbOptions.ApiBaseUrl.TrimEnd('/') + "/images/")
                };
            }).ToList();

            string contextString = string.Join("\n\n---\n\n", results.Select(x => $"[Belge: {x.SourceDocument} | Sayfa: {x.PageNumber}] {x.ContentSnippet}"));

            return (contextString, results);
        }
    }
}





