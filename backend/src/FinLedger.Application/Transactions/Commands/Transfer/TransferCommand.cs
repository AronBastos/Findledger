namespace FinLedger.Application.Transactions.Commands.Transfer;

using MediatR;

public record TransferCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string? Description) : IRequest<TransferResult>;

public record TransferResult(Guid DebitTransactionId, Guid CreditTransactionId);
