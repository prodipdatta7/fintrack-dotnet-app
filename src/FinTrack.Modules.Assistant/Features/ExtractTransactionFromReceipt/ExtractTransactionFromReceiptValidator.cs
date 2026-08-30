using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ExtractTransactionFromReceipt;

public sealed class ExtractTransactionFromReceiptValidator : AbstractValidator<ExtractTransactionFromReceiptCommand>
{
    public ExtractTransactionFromReceiptValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ImageBase64) || !string.IsNullOrWhiteSpace(x.RawText))
            .WithMessage("Either image data or receipt text must be provided.");
    }
}
