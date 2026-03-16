using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Samwin.UmlautConverter.Api.Settings;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Samwin.UmlautConverter.Api.Services
{
    /// <summary>
    /// Implements token creation logic using strongly-typed JWT settings.
    /// </summary>
    public class TokenGeneratorService : ICreateTokenService
    {
        private readonly JwtSettings _jwtSettings;

        public TokenGeneratorService(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public string CreateToken(string email, string subject, IEnumerable<string>? roles = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject), // Subject (User ID)
                new Claim(JwtRegisteredClaimNames.Email, email), // Email
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token ID
                new Claim(ClaimTypes.Name, email) // For compatibility with User.Identity.Name
            };

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role)); // Add role claims
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.TokenKey));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1), // Example: Token is valid for 1 hour
                Issuer = _jwtSettings.TokenIssuer,
                Audience = _jwtSettings.TokenAudience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}