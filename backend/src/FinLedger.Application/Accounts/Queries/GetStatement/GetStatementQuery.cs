namespace FinLedger.Application.Accounts.Queries.GetStatement;

using MediatR;
using FinLedger.Application.Common.Models;

public record GetStatementQuery(
    Guid AccountId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<StatementLineDto>>;

// PATTERN: CQRS DTO — estrutura de leitura independente da entidade de domínio.
// Inclui RunningBalance que é calculado via window function no banco,
// campo que não existe diretamente na entidade Account ou Transaction.
public record StatementLineDto(
    Guid TransactionId,
    string Type,
    decimal Amount,
    decimal RunningBalance,
    string? Description,
    DateTime CreatedAt);
