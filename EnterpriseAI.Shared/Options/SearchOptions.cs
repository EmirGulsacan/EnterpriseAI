namespace EnterpriseAI.Shared.Options
{
    public class SearchOptions
    {
        public const string SectionName = "Search";

        /// <summary>
        /// Minimum cosine similarity score (0.0 - 1.0) for a chunk to be included in the context.
        /// Chunks below this threshold are discarded.
        /// </summary>
        public float MinSimilarityScore { get; set; } = 0.60f;

        /// <summary>
        /// Maximum number of top-matching chunks to include in the LLM context.
        /// </summary>
        public int TopK { get; set; } = 5;
    }
}
