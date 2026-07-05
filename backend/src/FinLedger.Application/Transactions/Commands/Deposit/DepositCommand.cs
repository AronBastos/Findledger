namespace FinLedger.Application.Transactions.Commands.Deposit;

using MediatR;

public record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string? Description) : IRequest<Guid>;
