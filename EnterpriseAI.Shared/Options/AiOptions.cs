namespace EnterpriseAI.Shared.Options
{
    public class AiOptions
    {
        public const string SectionName = "AiSettings";

        public string ActiveProvider { get; set; } = "Gemini";
        public string VisionPrompt { get; set; } = "Describe the UI elements, fields, and buttons visible in this screenshot in detail.";
    }
}
