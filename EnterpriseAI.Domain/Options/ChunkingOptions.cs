namespace EnterpriseAI.Domain.Options
{
    public class ChunkingOptions
    {
        public const string SectionName = "Chunking";



        public int ChunkSize { get; set; } = 1000;




        public int ChunkOverlap { get; set; } = 200;




        public int MinChunkLength { get; set; } = 50;
    }
}

