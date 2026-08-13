using FluentValidation;

namespace FinTrack.Modules.Dashboard.Features.GetCashflowSeries;

/// <summary>
/// TASK-027. Runs through the MediatR ValidationBehavior, which executes for any request
/// that has a registered validator — reads included — so this guards the GET endpoint.
/// </summary>
public sealed class GetCashflowSeriesValidator : AbstractValidator<GetCashflowSeriesQuery>
{
    public GetCashflowSeriesValidator()
    {
        RuleFor(x => x.Timeframe)
            .Must(CashflowBuckets.IsAllowedTimeframe)
            .WithMessage("Timeframe must be one of: 7D, 15D, 30D, 60D, 6M, 1Y, Custom.");

        RuleFor(x => x.From)
            .NotNull()
            .When(x => CashflowBuckets.IsCustom(x.Timeframe))
            .WithMessage("Custom timeframe requires 'from'.");

        RuleFor(x => x.To)
            .NotNull()
            .When(x => CashflowBuckets.IsCustom(x.Timeframe))
            .WithMessage("Custom timeframe requires 'to'.");

        RuleFor(x => x)
            .Must(x => x.From!.Value.Date <= x.To!.Value.Date)
            .When(x => CashflowBuckets.IsCustom(x.Timeframe) && x.From.HasValue && x.To.HasValue)
            .WithMessage("'from' must be on or before 'to'.");

        RuleFor(x => x)
            .Must(x => (x.To!.Value.Date - x.From!.Value.Date).TotalDays <= CashflowBuckets.MaxCustomRangeDays)
            .When(x => CashflowBuckets.IsCustom(x.Timeframe)
                && x.From.HasValue
                && x.To.HasValue
                && x.From.Value.Date <= x.To.Value.Date)
            .WithMessage($"Custom range cannot exceed {CashflowBuckets.MaxCustomRangeDays} days.");
    }
}
