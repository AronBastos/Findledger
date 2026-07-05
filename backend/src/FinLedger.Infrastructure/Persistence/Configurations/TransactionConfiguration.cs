namespace FinLedger.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinLedger.Domain.Entities;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t =>
        {
            // PATTERN: Check Constraint no banco de dados
            // Mesmo que a aplicação valide amount > 0, a constraint no banco é a última
            // linha de defesa. Se um bug no código tentar inserir amount <= 0,
            // o PostgreSQL recusa com um erro — nenhum dado inválido entra.
            t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(t => t.CounterpartyAccountId).HasColumnName("counterparty_account_id");
        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();
        builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        // PATTERN: Composite Index para extrato e balance
        // A query mais comum é: WHERE account_id = ? ORDER BY created_at DESC
        // Sem este índice, o banco faria um full table scan em cada consulta de extrato.
        // Com o índice composto (account_id, created_at), o banco resolve a query
        // percorrendo apenas as linhas daquele account_id, já ordenadas.
        builder.HasIndex(t => new { t.AccountId, t.CreatedAt })
            .HasDatabaseName("ix_transactions_account_created");

        builder.HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
