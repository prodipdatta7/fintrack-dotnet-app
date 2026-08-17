using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Categories.Features.UnassignTagFromCategory;

public sealed record UnassignTagFromCategoryCommand(string CategoryId, string Tag) : IRequest<Result>;
