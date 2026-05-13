namespace EnterpriseAI.Shared.Options
{
    public class ChunkingOptions
    {
        public const string SectionName = "Chunking";

        /// <summary>
        /// Maximum character count per text chunk.
        /// </summary>
        public int ChunkSize { get; set; } = 1000;

        /// <summary>
        /// Number of characters from the end of the previous chunk
        /// to include at the beginning of the next chunk (sliding window overlap).
        /// </summary>
        public int ChunkOverlap { get; set; } = 200;

        /// <summary>
        /// Minimum character count for a chunk to be considered valid.
        /// Chunks shorter than this will be merged or discarded.
        /// </summary>
        public int MinChunkLength { get; set; } = 50;
    }
}
