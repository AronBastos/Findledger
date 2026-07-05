namespace FinLedger.Infrastructure.Persistence.Interceptors;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FinLedger.Domain.Entities;

// PATTERN: SaveChanges Interceptor para Audit Log
//
// Como funciona:
// 1. Sobrescrevemos SavingChangesAsync — chamado ANTES do SQL ser enviado ao banco
// 2. Percorremos o ChangeTracker para encontrar entidades Added/Modified/Deleted
// 3. Capturamos os valores originais (antes) e atuais (depois) como JSON
// 4. Adicionamos entradas AuditLog ao contexto
// 5. Essas entradas são salvas na MESMA transação de banco — o audit log é atômico
//    com a operação auditada: ou ambos persistem, ou nenhum persiste.
//
// Por que interceptor e não trigger SQL?
// - O código fica em C#, onde é testável, versionado e legível
// - Pode acessar contexto da aplicação (ex: usuário logado, correlation ID)
// - Sem dependência de stored procedures ou triggers específicos do banco
//
// Por que interceptor e não domain events?
// - Interceptor captura automaticamente toda mudança, sem precisar emitir evento por entidade
// - Menos boilerplate para entidades simples
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, ct);

        var auditEntries = BuildAuditEntries(eventData.Context);

        if (auditEntries.Count > 0)
            eventData.Context.Set<AuditLog>().AddRange(auditEntries);

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static List<AuditLog> BuildAuditEntries(DbContext context)
    {
        var entries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Evitar loop infinito: não auditamos a própria tabela de auditoria
            if (entry.Entity is AuditLog) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var action = entry.State switch
            {
                EntityState.Added    => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted  => "DELETE",
                _                    => "UNKNOWN"
            };

            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            var entityId  = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())
                ?.CurrentValue?.ToString() ?? string.Empty;

            // Para UPDATE: capturamos apenas as propriedades que mudaram
            var oldValues = entry.State is EntityState.Modified or EntityState.Deleted
                ? JsonSerializer.Serialize(
                    entry.Properties
                        .Where(p => p.IsModified || entry.State == EntityState.Deleted)
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                    JsonOptions)
                : null;

            var newValues = entry.State is EntityState.Added or EntityState.Modified
                ? JsonSerializer.Serialize(
                    entry.Properties
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                    JsonOptions)
                : null;

            entries.Add(AuditLog.Create(tableName, action, entityId, oldValues, newValues));
        }

        return entries;
    }
}
