namespace FinLedger.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using FinLedger.Application.Transactions.Commands.Deposit;
using FinLedger.Application.Transactions.Commands.Withdraw;
using FinLedger.Application.Transactions.Commands.Transfer;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("deposit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit(
        [FromBody] DepositCommand command,
        CancellationToken ct)
    {
        var transactionId = await _mediator.Send(command, ct);
        return StatusCode(201, new { transactionId });
    }

    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(
        [FromBody] WithdrawCommand command,
        CancellationToken ct)
    {
        var transactionId = await _mediator.Send(command, ct);
        return StatusCode(201, new { transactionId });
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return StatusCode(201, result);
    }
}
