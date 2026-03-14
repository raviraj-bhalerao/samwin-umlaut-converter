using System;

namespace Samwin.UmlautConverter.Api.Services
{
    public class EnvironmentAuthConfigProvider : IAuthConfigProvider
    {
        private const string KeyEnvVar = "TokenKey";
        private const string IssuerEnvVar = "TokenIssuer";
        private const string AudienceEnvVar = "TokenAudience";
        private const int MinimumKeyLength = 32;

        public string GetSecretKey()
        {
            var key = Environment.GetEnvironmentVariable(KeyEnvVar);
            
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException($"Security critical error: Environment variable '{KeyEnvVar}' is not set.");

            if (key.Length < MinimumKeyLength)
                throw new InvalidOperationException($"Security critical error: '{KeyEnvVar}' must be at least {MinimumKeyLength} characters.");

            return key;
        }

        public string GetIssuer()
        {
            return Environment.GetEnvironmentVariable(IssuerEnvVar) 
               ?? throw new InvalidOperationException($"Security critical error: '{IssuerEnvVar}' is not set.");
        }

        public string GetAudience()
        {
            return Environment.GetEnvironmentVariable(AudienceEnvVar) 
               ?? throw new InvalidOperationException($"Security critical error: '{AudienceEnvVar}' is not set.");
        }
    }
}