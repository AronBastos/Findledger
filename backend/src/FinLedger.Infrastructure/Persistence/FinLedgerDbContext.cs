namespace FinLedger.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FinLedger.Domain.Entities;

public sealed class FinLedgerDbContext : DbContext
{
    public FinLedgerDbContext(DbContextOptions<FinLedgerDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carrega todas as IEntityTypeConfiguration<T> do assembly automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinLedgerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
