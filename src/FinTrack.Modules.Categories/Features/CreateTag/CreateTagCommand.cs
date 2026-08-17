using FinTrack.BuildingBlocks;
using FinTrack.Modules.Categories.Features.GetTags;
using MediatR;

namespace FinTrack.Modules.Categories.Features.CreateTag;

public sealed record CreateTagCommand(string Name) : IRequest<Result<TagDto>>;
