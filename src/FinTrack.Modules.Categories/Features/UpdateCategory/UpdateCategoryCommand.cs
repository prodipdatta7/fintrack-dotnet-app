using FinTrack.BuildingBlocks;
using FinTrack.Modules.Categories.Domain;
using MediatR;

namespace FinTrack.Modules.Categories.Features.UpdateCategory;

public sealed record UpdateCategoryCommand(
    string Id,
    string Name,
    CategoryType Type,
    string Icon,
    string Color) : IRequest<Result>;
