namespace FinLedger.Application.Transactions.Commands.Transfer;

using FluentValidation;

public sealed class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty();
        RuleFor(x => x.SourceAccountId)
            .NotEqual(x => x.DestinationAccountId)
            .WithMessage("Source and destination accounts must be different.");
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
