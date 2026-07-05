namespace FinLedger.Application.Accounts.Commands.CloseAccount;

using MediatR;

public record CloseAccountCommand(Guid AccountId) : IRequest;
