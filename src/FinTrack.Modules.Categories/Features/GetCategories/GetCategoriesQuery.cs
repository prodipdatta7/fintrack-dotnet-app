using FinTrack.BuildingBlocks;
using FinTrack.Modules.Categories.Domain;
using MediatR;

namespace FinTrack.Modules.Categories.Features.GetCategories;

public sealed record GetCategoriesQuery(CategoryType? Type = null) : IRequest<Result<List<CategoryDto>>>;

public sealed record CategoryDto(
    string Id,
    string Name,
    CategoryType Type,
    string Icon,
    string Color,
    bool IsDefault);
