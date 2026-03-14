using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Samwin.UmlautConverter.Api.Services;
using Xunit;

namespace Samwin.UmlautConverter.Api.Tests
{
    public class TokenGeneratorServiceTests
    {
        private readonly IAuthConfigProvider _fakeAuthConfig;
        private readonly TokenGeneratorService _service;

        public TokenGeneratorServiceTests()
        {
            _fakeAuthConfig = new FakeAuthConfigProvider();
            _service = new TokenGeneratorService(_fakeAuthConfig);
        }

        [Fact]
        public void CreateToken_GeneratesValidJwtToken()
        {
            // Arrange
            var email = "test@example.com";
            var subject = "test-subject-123";

            // Act
            var tokenString = _service.CreateToken(email, subject);

            // Assert
            Assert.NotNull(tokenString);
            Assert.NotEmpty(tokenString);

            var handler = new JwtSecurityTokenHandler();
            Assert.True(handler.CanReadToken(tokenString));

            var jwtToken = handler.ReadJwtToken(tokenString);
            
            // Verify Standard Claims (Issuer, Audience)
            Assert.Equal(_fakeAuthConfig.GetIssuer(), jwtToken.Issuer);
            Assert.Equal(_fakeAuthConfig.GetAudience(), jwtToken.Audiences.FirstOrDefault());

            // Verify Payload Claims
            // Note: We check by value because claim types (e.g. "nameid" vs ClaimTypes.NameIdentifier) 
            // can vary depending on inbound mapping settings.
            Assert.Contains(jwtToken.Claims, c => c.Value == email);
            Assert.Contains(jwtToken.Claims, c => c.Value == subject);
        }

        [Fact]
        public void CreateToken_WithRoles_AddsRoleClaims()
        {
            // Arrange
            var email = "admin@example.com";
            var subject = "admin-user";
            var roles = new[] { "Admin", "Supervisor", "Agent" };

            // Act
            var tokenString = _service.CreateToken(email, subject, roles);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            // Filter claims where the Type ends with "role" (handles both short 'role' and long SOAP URI formats)
            var roleClaims = jwtToken.Claims
                .Where(c => c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .ToList();

            Assert.Equal(3, roleClaims.Count);
            Assert.Contains("Admin", roleClaims);
            Assert.Contains("Supervisor", roleClaims);
            Assert.Contains("Agent", roleClaims);
        }

        private class FakeAuthConfigProvider : IAuthConfigProvider
        {
            // Key must be at least 32 chars for HmacSha256
            public string GetSecretKey() => "TEST_SECRET_KEY_MUST_BE_VERY_LONG_FOR_HMAC_SHA256_VALIDATION";
            public string GetIssuer() => "test-suite-issuer";
            public string GetAudience() => "test-suite-audience";
        }
    }
}