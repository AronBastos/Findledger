namespace FinLedger.Application.Transactions.Commands.Withdraw;

using FluentValidation;

public sealed class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
