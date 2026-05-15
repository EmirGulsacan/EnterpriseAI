using System.Text.Json;
using Pgvector;

namespace EnterpriseAI.Domain.Entities
{
    public class DocumentChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ApplicationCode { get; set; }
        public string ModuleCode { get; set; }
        public string Content { get; set; }
        public int PageNumber { get; set; }
        public Vector? Embedding { get; set; }
        public JsonDocument? Metadata { get; set; }
        public bool IsImageDescription { get; set; }
        public string SourceDocument { get; set; }
        public string FileHash { get; set; }
    }
}


