using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateAccount;

public sealed record ProposeCreateAccountCommand(
    string Name,
    string Type,
    decimal? InitialBalance = 0,
    string? Currency = "BDT",
    string? Color = "#6366f1") : IRequest<Result<ProposedActionDto<ProposedCreateAccountPayload>>>;
