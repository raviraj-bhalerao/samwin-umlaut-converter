using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samwin.UmlautConverter.Api.Services;
namespace Samwin.UmlautConverter.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly ICreateTokenService _tokenService;
        private readonly IActivityService _activityService;
        private readonly MetricsService _metricsService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(ICreateTokenService tokenService, IActivityService activityService,
            MetricsService metricsService, ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
            _activityService = activityService;
            _metricsService = metricsService;
        }
        [ExcludeFromCodeCoverage]
        [HttpPost(nameof(GoogleLogin))]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            _metricsService.RequestCounter.Add(1, new TagList
            {
                {"endpoint", nameof(GoogleLogin)}
            });

            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "GoogleLogin" }, { "OperationId", Guid.NewGuid() } }))
            {
                _metricsService.LoginAttempts.Add(1, new TagList
                {
                    {"result", "success"}
                });
                _logger.LogInformation("Google login request received");
                // 1. Verify Google ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);

                // 2. Create your own JWT with specific roles
                var roles = new[] { "GoogleUser", "Agent" };
                var jwt = _tokenService.CreateToken(payload.Email, payload.Subject, roles);

                _logger.LogInformation("JWT created using google token");

                return Ok(new { token = jwt });
            }
        }

        [HttpPost(nameof(LoginWithPassword))]
        public async Task<IActionResult> LoginWithPassword([FromBody] LoginRequest request)
        {
            _metricsService.RequestCounter.Add(1, new TagList
            {
                {"endpoint", nameof(LoginWithPassword)}
            });
            var users = new List<UserRecord>
            {
                new UserRecord("bhaleraor", "bhaleraor@1234", new[] { "TechCaptain", "Administrator" }),
                new UserRecord("behrs", "behrs@1234", new[] { "Manager", "Approver" }),
                // Add more users here
            };
            using (_logger.BeginScope(new Dictionary<string, object> { { "Scope", "LoginWithPassword" }, { "OperationId", Guid.NewGuid() } }))
            {
                _logger.LogInformation("Login request received");
                UserRecord? user;
                using (var validateActivity = _activityService.StartActivity("ValidateUserCredentials"))
                {
                    user = users.FirstOrDefault(user => user.UserName == request.UserName && user.Password == request.Password);
                    _metricsService.LoginAttempts.Add(1, new TagList
                    {
                        {"result", user != null ? "success" : "failure"}
                    });
                    _logger.LogInformation("User cred validated");

                    if (user != null)
                    {
                        using (var tokenCreationActivity = _activityService.StartActivity("GenerateJwt"))
                        {
                            var sw = Stopwatch.StartNew();
                            var jwt = _tokenService.CreateToken(user.UserName, user.UserName, user.Roles);
                            sw.Stop();
                            _metricsService.JwtGenerationTime.Record(sw.Elapsed.TotalMilliseconds);
                            _logger.LogInformation("JWT created using user creds");
                            return Ok(new { token = jwt });
                        }
                    }
                    else
                    {
                        _logger.LogError("User creds do not match.");
                        return Unauthorized();
                    }
                }
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