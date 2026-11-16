namespace FinLedger.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Usada apenas pelas ferramentas do EF Core (dotnet ef migrations add/update).
/// Evita subir o host da API — que aplicaria migrations e abriria conexão — só para gerar uma migration.
/// </summary>
public sealed class FinLedgerDbContextFactory : IDesignTimeDbContextFactory<FinLedgerDbContext>
{
    public FinLedgerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=finledger;Username=finledger;Password=finledger123";

        var options = new DbContextOptionsBuilder<FinLedgerDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FinLedgerDbContext(options);
    }
}
