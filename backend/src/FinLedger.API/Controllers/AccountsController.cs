namespace FinLedger.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using FinLedger.Application.Accounts.Commands.CreateAccount;
using FinLedger.Application.Accounts.Commands.CloseAccount;
using FinLedger.Application.Accounts.Queries.GetAllAccounts;
using FinLedger.Application.Accounts.Queries.GetStatement;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountCommand command,
        CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetStatement), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] string ownerName,
        CancellationToken ct)
    {
        var accounts = await _mediator.Send(new GetAllAccountsQuery(ownerName), ct);
        return Ok(accounts);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseAccount(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CloseAccountCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/statement")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatement(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStatementQuery(id, page, pageSize), ct);
        return Ok(result);
    }
}
