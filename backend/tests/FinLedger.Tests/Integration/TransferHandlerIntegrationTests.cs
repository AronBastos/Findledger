namespace FinLedger.Tests.Integration;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using FinLedger.Application.Transactions.Commands.Transfer;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using FinLedger.Infrastructure.Persistence;
using FinLedger.Infrastructure.Persistence.Interceptors;
using FinLedger.Infrastructure.Persistence.Repositories;

// Regressão: exercita o TransferCommandHandler REAL com a mesma configuração da API
// (EnableRetryOnFailure ativo) e o extrato via Dapper — caminhos que os testes que
// chamam repositórios diretamente não cobrem.
public sealed class TransferHandlerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private FinLedgerDbContext _context = null!;
    private UnitOfWork _uow = null!;
    private AccountRepository _accounts = null!;
    private TransactionRepository _transactions = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<FinLedgerDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .AddInterceptors(new AuditInterceptor())
            .Options;

        _context = new FinLedgerDbContext(options);
        await _context.Database.MigrateAsync();

        _uow = new UnitOfWork(_context);
        _accounts = new AccountRepository(_context);
        _transactions = new TransactionRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Transfer_ThroughHandler_MovesMoneyAndShowsInStatement()
    {
        var source = Account.Create("test-user", "Origem", AccountType.Checking);
        var dest = Account.Create("test-user", "Destino", AccountType.Savings);
        source.Credit(1000m);
        await _accounts.AddAsync(source);
        await _accounts.AddAsync(dest);
        await _uow.CommitAsync();

        var handler = new TransferCommandHandler(_accounts, _transactions, _uow);
        await handler.Handle(new TransferCommand(source.Id, dest.Id, 300m, "teste"), CancellationToken.None);

        _context.ChangeTracker.Clear();
        (await _context.Accounts.FindAsync(source.Id))!.Balance.Should().Be(700m);
        (await _context.Accounts.FindAsync(dest.Id))!.Balance.Should().Be(300m);

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        var statement = await new StatementRepository(connection)
            .GetStatementAsync(source.Id, page: 1, pageSize: 10);

        statement.TotalCount.Should().Be(1);
        statement.Items.Should().ContainSingle(l => l.Amount == 300m);
    }

    [Fact]
    public async Task Transfer_ThroughHandler_WithInsufficientFunds_RollsBackEverything()
    {
        var source = Account.Create("test-user", "Origem", AccountType.Checking);
        var dest = Account.Create("test-user", "Destino", AccountType.Savings);
        source.Credit(100m);
        await _accounts.AddAsync(source);
        await _accounts.AddAsync(dest);
        await _uow.CommitAsync();

        var handler = new TransferCommandHandler(_accounts, _transactions, _uow);
        var act = () => handler.Handle(new TransferCommand(source.Id, dest.Id, 500m, null), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();

        _context.ChangeTracker.Clear();
        (await _context.Accounts.FindAsync(source.Id))!.Balance.Should().Be(100m);
        (await _context.Accounts.FindAsync(dest.Id))!.Balance.Should().Be(0m);
        (await _context.Transactions.CountAsync()).Should().Be(0);
    }
}
