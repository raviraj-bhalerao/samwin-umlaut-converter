using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Samwin.UmlautConverter.Api.Services;
namespace Samwin.UmlautConverter.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly ICreateTokenService _tokenService;
        public AuthController(ICreateTokenService tokenService)
        {
            _tokenService = tokenService;
        }
        [ExcludeFromCodeCoverage]
        [HttpPost(nameof(GoogleLogin))]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            // 1. Verify Google ID token
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);

            // 2. Create your own JWT with specific roles
            var roles = new[] { "GoogleUser", "Agent" };
            var jwt = _tokenService.CreateToken(payload.Email, payload.Subject, roles);

            return Ok(new { token = jwt });
        }

        [HttpPost(nameof(LoginWithPassword))]
        public async Task<IActionResult> LoginWithPassword([FromBody] LoginRequest request)
        {
            var users = new List<UserRecord>
            {
                new UserRecord("bhaleraor", "bhaleraor@1234", new[] { "TechCaptain", "Administrator" }),
                new UserRecord("behrs", "behrs@1234", new[] { "Manager", "Approver" }),
                // Add more users here
            };

            var user = users.FirstOrDefault(user => user.UserName == request.UserName && user.Password == request.Password);

            if (user != null)
            {
                var jwt = _tokenService.CreateToken(user.UserName, user.UserName, user.Roles);
                return Ok(new { token = jwt });
            }
            else
            {
                return Unauthorized();
            }
        }

    }

    [ExcludeFromCodeCoverage]
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    [ExcludeFromCodeCoverage]
    public record UserRecord(string UserName, string Password, string[] Roles);
    [ExcludeFromCodeCoverage]
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }
}