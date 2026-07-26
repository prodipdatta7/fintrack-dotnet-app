using FinTrack.BuildingBlocks;
using MongoDB.Entities;

namespace FinTrack.Modules.Users.Domain;

[Collection("refreshTokens")]
public class RefreshToken : Entity
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
