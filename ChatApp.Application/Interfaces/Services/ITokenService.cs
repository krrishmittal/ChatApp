using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}