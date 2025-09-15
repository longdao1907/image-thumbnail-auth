using AuthAPI.Core.Domain.Entities;

namespace AuthAPI.Core.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(ApplicationUser applicationUser);
        string GenerateServiceToken(string clientId, string clientSecret);
    }
}
