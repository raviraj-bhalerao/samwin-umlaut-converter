using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Samwin.UmlautConverter.Api.Services
{
    public class TokenGeneratorService : ICreateTokenService
    {
        private readonly IAuthConfigProvider _authConfig;

        public TokenGeneratorService(IAuthConfigProvider authConfig)
        {
            _authConfig = authConfig;
        }

        public string CreateToken(string email, string subject, IEnumerable<string>? roles = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, subject)
            };

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var secretKey = _authConfig.GetSecretKey();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _authConfig.GetIssuer(),
                audience: _authConfig.GetAudience(),
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}