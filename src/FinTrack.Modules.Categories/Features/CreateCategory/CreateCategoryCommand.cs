using FinTrack.BuildingBlocks;
using FinTrack.Modules.Categories.Domain;
using MediatR;

namespace FinTrack.Modules.Categories.Features.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    CategoryType Type,
    string Icon,
    string Color) : IRequest<Result<string>>;
