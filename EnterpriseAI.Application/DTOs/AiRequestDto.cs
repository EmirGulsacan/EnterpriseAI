namespace EnterpriseAI.Application.DTOs
{
    public class AiRequestDto
    {
        public string? ApplicationCode { get; set; }
        public string? TargetDocument { get; set; }
        public string? SystemInstruction { get; set; }
        public string? ContextData { get; set; }
        public string? UserPrompt { get; set; }
    }
}

