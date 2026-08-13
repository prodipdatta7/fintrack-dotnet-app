using MediatR;

namespace FinTrack.Contracts.Commands;

/// <summary>
/// Applies a signed ledger delta to an account balance in-process (income = +, expense = −).
/// Must run after the triggering transaction row is persisted so an unsynced backfill sees it.
/// </summary>
public sealed record ApplyAccountBalanceDeltaCommand(
    string AccountId,
    string UserId,
    decimal SignedDelta) : IRequest;
