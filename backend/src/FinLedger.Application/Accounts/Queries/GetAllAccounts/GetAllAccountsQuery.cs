namespace FinLedger.Application.Accounts.Queries.GetAllAccounts;

using MediatR;

public record GetAllAccountsQuery(string OwnerName) : IRequest<IReadOnlyList<AccountSummaryDto>>;

public record AccountSummaryDto(
    Guid Id,
    string Name,
    string Type,
    decimal Balance,
    DateTime CreatedAt);
