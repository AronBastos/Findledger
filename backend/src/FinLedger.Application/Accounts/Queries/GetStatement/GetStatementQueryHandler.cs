namespace FinLedger.Application.Accounts.Queries.GetStatement;

using MediatR;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Application.Common.Models;

public sealed class GetStatementQueryHandler
    : IRequestHandler<GetStatementQuery, PagedResult<StatementLineDto>>
{
    private readonly IStatementRepository _statements;

    public GetStatementQueryHandler(IStatementRepository statements) => _statements = statements;

    public Task<PagedResult<StatementLineDto>> Handle(
        GetStatementQuery request, CancellationToken ct)
        => _statements.GetStatementAsync(request.AccountId, request.Page, request.PageSize, ct);
}
