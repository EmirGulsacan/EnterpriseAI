namespace EnterpriseAI.Domain.Options
{
    public class SearchOptions
    {
        public const string SectionName = "Search";




        public float MinSimilarityScore { get; set; } = 0.60f;



        public int TopK { get; set; } = 5;
    }
}

