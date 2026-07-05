namespace FinLedger.Domain.Entities;

// PATTERN: Audit Log
// Registra automaticamente toda mudança de estado nas entidades.
// Escrito exclusivamente pelo AuditInterceptor — nunca diretamente pelo código da aplicação.
// Armazenado como JSONB no PostgreSQL para queries flexíveis sobre os valores.
public sealed class AuditLog
{
    public long Id { get; private set; }
    public string TableName { get; private set; } = default!;
    public string Action { get; private set; } = default!;   // INSERT | UPDATE | DELETE
    public string EntityId { get; private set; } = default!;
    public string? OldValues { get; private set; }            // JSON snapshot antes da mudança
    public string? NewValues { get; private set; }            // JSON snapshot depois da mudança
    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string tableName,
        string action,
        string entityId,
        string? oldValues,
        string? newValues) => new()
    {
        TableName = tableName,
        Action = action,
        EntityId = entityId,
        OldValues = oldValues,
        NewValues = newValues,
        CreatedAt = DateTime.UtcNow
    };
}
