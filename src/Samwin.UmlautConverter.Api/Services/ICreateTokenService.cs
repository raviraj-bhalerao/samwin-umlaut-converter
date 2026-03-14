using System.Collections.Generic;

namespace Samwin.UmlautConverter.Api.Services
{
    public interface ICreateTokenService
    {
        string CreateToken(string email, string subject, IEnumerable<string>? roles = null);
    }
}