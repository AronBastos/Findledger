namespace FinLedger.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinLedger.Domain.Entities;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.OwnerName).HasColumnName("owner_name").HasMaxLength(150).IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(18,2)")
            .IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");

        // PATTERN: Optimistic Concurrency via xmin do PostgreSQL
        // O Npgsql mapeia a coluna de sistema 'xmin' (transaction ID) como token de concorrência.
        // A cada UPDATE na linha, o banco atualiza xmin automaticamente — sem coluna extra.
        // O EF Core inclui: WHERE id = @id AND xmin = @rowVersion no UPDATE.
        // Se outra transação já modificou a linha → xmin mudou → 0 rows affected
        // → DbUpdateConcurrencyException.
        builder.Property(a => a.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();

        // PATTERN: Global Query Filter para Soft Delete
        // Toda query LINQ em Accounts adiciona automaticamente WHERE deleted_at IS NULL.
        // Não é necessário nenhum .Where(a => a.DeletedAt == null) no código.
        // Para ignorar o filtro (ex: admin panel): use .IgnoreQueryFilters()
        builder.HasQueryFilter(a => a.DeletedAt == null);

        // PATTERN: Partial Index — apenas contas ativas
        // Um índice parcial é menor e mais rápido que um índice total.
        // Buscas por owner_name em contas ativas (o caso mais comum) beneficiam deste índice.
        // O banco só mantém no índice as linhas onde deleted_at IS NULL.
        builder.HasIndex(a => a.OwnerName)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_accounts_owner_active");
    }
}
