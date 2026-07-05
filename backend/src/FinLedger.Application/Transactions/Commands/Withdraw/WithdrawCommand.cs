namespace FinLedger.Application.Transactions.Commands.Withdraw;

using MediatR;

public record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string? Description) : IRequest<Guid>;
