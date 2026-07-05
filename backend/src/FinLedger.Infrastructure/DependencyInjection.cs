namespace FinLedger.Infrastructure;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Interfaces;
using FinLedger.Infrastructure.Persistence;
using FinLedger.Infrastructure.Persistence.Interceptors;
using FinLedger.Infrastructure.Persistence.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        // AuditInterceptor é Singleton pois não tem estado por request
        services.AddSingleton<AuditInterceptor>();

        services.AddDbContext<FinLedgerDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                // PATTERN: Retry automático em falhas transitórias
                // Reconecta automaticamente em erros de rede ou timeouts temporários
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });

            // Adiciona o interceptor de auditoria ao contexto
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // PATTERN: Connection Pool (Npgsql)
        // IDbConnection é Scoped — cada request HTTP usa a mesma conexão para EF Core e Dapper,
        // evitando abrir dois slots do pool por request.
        // O Npgsql gerencia um pool de conexões automaticamente (padrão: max 100 conexões).
        // Configuração do pool via connection string:
        //   "Minimum Pool Size=1;Maximum Pool Size=20;Connection Idle Lifetime=300"
        services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

        // Repositórios de escrita (EF Core — com change tracking)
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Repositório de leitura (Dapper — sem change tracking, SQL direto)
        services.AddScoped<IStatementRepository, StatementRepository>();

        // Unit of Work envolve o DbContext
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
