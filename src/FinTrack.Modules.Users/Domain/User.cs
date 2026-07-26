using FinTrack.BuildingBlocks;
using MongoDB.Entities;

namespace FinTrack.Modules.Users.Domain;

[Collection("users")]
public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
