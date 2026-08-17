using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Categories.Features.AssignTagToCategory;

public sealed record AssignTagToCategoryCommand(string CategoryId, string Tag) : IRequest<Result>;
