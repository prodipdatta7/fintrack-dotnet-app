using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Categories.Features.GetTags;

public sealed record GetTagsQuery : IRequest<Result<List<TagDto>>>;

public sealed record TagDto(string Id, string Name);
