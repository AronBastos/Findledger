namespace FinLedger.Application.Accounts.Commands.CreateAccount;

using MediatR;
using FinLedger.Domain.Enums;

public record CreateAccountCommand(
    string OwnerName,
    string Name,
    AccountType Type) : IRequest<Guid>;
