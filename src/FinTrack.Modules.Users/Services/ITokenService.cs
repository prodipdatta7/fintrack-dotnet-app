using FinTrack.Modules.Users.Domain;

namespace FinTrack.Modules.Users.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
