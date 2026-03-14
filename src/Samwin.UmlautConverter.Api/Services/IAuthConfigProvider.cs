namespace Samwin.UmlautConverter.Api.Services
{
    /// <summary>
    /// Provides authentication configuration including Secret Key, Issuer, and Audience.
    /// </summary>
    public interface IAuthConfigProvider
    {
        string GetSecretKey();
        string GetIssuer();
        string GetAudience();
    }
}