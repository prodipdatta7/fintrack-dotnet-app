using FinTrack.BuildingBlocks;
using FinTrack.Modules.Categories.Features.GetCategories;
using MediatR;

namespace FinTrack.Modules.Categories.Features.GetCategory;

public sealed record GetCategoryQuery(string Id) : IRequest<Result<CategoryDto>>;
