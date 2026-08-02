using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.UploadAvatar;

public sealed record UploadAvatarCommand(
    Stream FileStream,
    string FileName,
    string ContentType) : IRequest<Result<UploadAvatarResponse>>;

public sealed record UploadAvatarResponse(string AvatarUrl);
