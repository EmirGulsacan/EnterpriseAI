using System.Collections.Generic;

namespace EnterpriseAI.Application.DTOs
{
    public class AiResponseDto
    {
        public string Answer { get; set; }
        public List<ChunkReferenceDto> References { get; set; } = new();
    }

    public class ChunkReferenceDto
    {
        public string SourceDocument { get; set; }
        public int PageNumber { get; set; }
        public string ImagePath { get; set; }
        public string ContentSnippet { get; set; }
    }
}

