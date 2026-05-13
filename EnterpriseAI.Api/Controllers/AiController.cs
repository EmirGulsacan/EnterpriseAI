using EnterpriseAI.Api.Models;
using EnterpriseAI.Api.Services;
using EnterpriseAI.Shared.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace EnterpriseAI.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiProvider _aiProvider;
        private readonly KnowledgeBaseService _kbService;

        public AiController(IAiProvider aiProvider, KnowledgeBaseService kbService)
        {
            _aiProvider = aiProvider;
            _kbService = kbService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiRequestDto request)
        {
            if (IsSecurityThreat(request.UserPrompt))
            {
                return BadRequest(new { Error = "Security violation detected." });
            }

            string autoContext = await _kbService.SearchHybridAsync(request.UserPrompt);
            string finalContext = string.IsNullOrWhiteSpace(request.ContextData) ? autoContext : request.ContextData;

            string baseInstruction = request.SystemInstruction ?? "You are a helpful corporate assistant.";
            string imageInstruction = "\n\nCRITICAL RULE: If the provided context contains markdown image links (e.g. ![Ekran Görüntüsü](/images/xyz.png)), you MUST include them exactly as they are in your output to visually support your answer. Do not just describe the image, print the exact markdown link!";
            string finalInstruction = baseInstruction + imageInstruction;

            var answer = await _aiProvider.GetAnswerAsync(
                finalInstruction,
                finalContext,
                request.UserPrompt
            );

            return Ok(new { Answer = answer });
        }

        private bool IsSecurityThreat(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string pattern = @"\b(drop\s+table|delete\s+from|truncate\s+table|exec\s+|insert\s+into|update\s+.*set)\b";
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
    }
}