
namespace EnterpriseAI.Shared.Models
{
    public class DocumentChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ApplicationCode { get; set; }
        public string ModuleCode { get; set; }
        public string Content { get; set; }
        public int PageNumber { get; set; }
        public float[] Embedding { get; set; }
        public bool IsImageDescription { get; set; }
    }
}