namespace FinLedger.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinLedger.Domain.Entities;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(a => a.TableName).HasColumnName("table_name").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(10).IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();

        // PATTERN: JSONB no PostgreSQL
        // JSONB (binary JSON) permite queries dentro do JSON via operadores @>, ->, ->>.
        // Por exemplo: SELECT * FROM audit_logs WHERE new_values->>'Balance' = '500.00'
        // Muito mais eficiente que TEXT para consultas sobre os dados auditados.
        builder.Property(a => a.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        // Índice para buscar histórico de uma entidade específica
        builder.HasIndex(a => new { a.TableName, a.EntityId })
            .HasDatabaseName("ix_audit_logs_entity");
    }
}
