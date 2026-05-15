using EnterpriseAI.Application.DTOs;
using EnterpriseAI.Application.Services;
using EnterpriseAI.Application.Interfaces;
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

            var searchResult = await _kbService.SearchHybridAsync(request);
            string finalContext = string.IsNullOrWhiteSpace(request.ContextData) ? searchResult.ContextString : request.ContextData;

            string baseInstruction = request.SystemInstruction ?? "You are a helpful corporate assistant.";
            string imageInstruction = "\n\nCRITICAL RULE: If the provided context contains markdown image links (e.g. ![Ekran Görüntüsü](/images/xyz.png)), you MUST include them exactly as they are in your output to visually support your answer. Do not just describe the image, print the exact markdown link!";
            string finalInstruction = baseInstruction + imageInstruction;

            var answer = await _aiProvider.GetAnswerAsync(
                finalInstruction,
                finalContext,
                request.UserPrompt
            );

            var response = new AiResponseDto
            {
                Answer = answer,
                References = searchResult.References
            };

            return Ok(response);
        }

        [HttpPost("stream")]
        public async Task Stream([FromBody] AiRequestDto request)
        {
            if (IsSecurityThreat(request.UserPrompt))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("data: {\"type\":\"error\",\"data\":\"Security violation detected.\"}\n\n");
                return;
            }

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var searchResult = await _kbService.SearchHybridAsync(request);

            var referencesJson = System.Text.Json.JsonSerializer.Serialize(new { type = "references", data = searchResult.References }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await Response.WriteAsync($"data: {referencesJson}\n\n");
            await Response.Body.FlushAsync();

            string finalContext = string.IsNullOrWhiteSpace(request.ContextData) ? searchResult.ContextString : request.ContextData;
            string baseInstruction = request.SystemInstruction ?? "You are a helpful corporate assistant.";
            string imageInstruction = "\n\nCRITICAL RULE: If the provided context contains markdown image links (e.g. ![Ekran Görüntüsü](/images/xyz.png)), you MUST include them exactly as they are in your output to visually support your answer. Do not just describe the image, print the exact markdown link!";
            string finalInstruction = baseInstruction + imageInstruction;

            try 
            {
                var stream = _aiProvider.GetAnswerStreamAsync(finalInstruction, finalContext, request.UserPrompt);
                await foreach (var chunk in stream)
                {
                    var contentJson = System.Text.Json.JsonSerializer.Serialize(new { type = "content", data = chunk });
                    await Response.WriteAsync($"data: {contentJson}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                var errorJson = System.Text.Json.JsonSerializer.Serialize(new { type = "error", data = ex.Message });
                await Response.WriteAsync($"data: {errorJson}\n\n");
                await Response.Body.FlushAsync();
            }

            await Response.WriteAsync("data: {\"type\":\"done\"}\n\n");
            await Response.Body.FlushAsync();
        }

        private bool IsSecurityThreat(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string pattern = @"\b(drop\s+table|delete\s+from|truncate\s+table|exec\s+|insert\s+into|update\s+.*set)\b";
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
    }
}

