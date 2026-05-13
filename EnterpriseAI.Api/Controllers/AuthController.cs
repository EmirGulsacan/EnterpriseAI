using EnterpriseAI.Api.Models;
using EnterpriseAI.Shared.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EnterpriseAI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly JwtOptions _jwtOptions;

        public AuthController(IConfiguration configuration, IOptions<JwtOptions> jwtOptions)
        {
            _configuration = configuration;
            _jwtOptions = jwtOptions.Value;
        }

        [HttpPost("token")]
        public IActionResult GenerateToken([FromBody] TokenRequestDto request)
        {
            var expectedSecret = _configuration[$"AllowedClients:{request.ClientId}"];

            if (!string.IsNullOrEmpty(expectedSecret) && expectedSecret == request.ClientSecret)
            {
                var key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, request.ClientId) }),
                    Expires = DateTime.UtcNow.AddHours(_jwtOptions.ExpirationInHours),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = new JwtSecurityTokenHandler().CreateToken(tokenDescriptor);
                return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
            }

            return Unauthorized(new { Message = "Invalid ClientId or ClientSecret." });
        }
    }
}