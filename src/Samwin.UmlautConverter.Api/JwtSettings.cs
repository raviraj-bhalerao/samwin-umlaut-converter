using System.ComponentModel.DataAnnotations;

namespace Samwin.UmlautConverter.Api.Settings
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        [Required(AllowEmptyStrings = false)]
        [MinLength(32, ErrorMessage = "TokenKey must be at least 32 characters long for HS256 security.")]
        public string TokenKey { get; init; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string TokenIssuer { get; init; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string TokenAudience { get; init; } = string.Empty;
    }
}

