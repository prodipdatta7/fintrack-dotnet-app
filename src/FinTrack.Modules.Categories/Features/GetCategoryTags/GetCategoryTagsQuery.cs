using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Categories.Features.GetCategoryTags;

public sealed record GetCategoryTagsQuery(string CategoryId) : IRequest<Result<List<string>>>;
