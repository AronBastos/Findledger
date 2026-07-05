namespace FinLedger.API.Middleware;

using System.Text.Json;
using FluentValidation;
using FinLedger.Domain.Exceptions;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteJsonAsync(context, new { title = "Validation failed", errors });
        }
        catch (AccountNotFoundException ex)
        {
            context.Response.StatusCode = 404;
            await WriteJsonAsync(context, new { title = ex.Message });
        }
        catch (InsufficientFundsException ex)
        {
            context.Response.StatusCode = 422;
            await WriteJsonAsync(context, new
            {
                title = "Insufficient funds",
                detail = ex.Message,
                currentBalance = ex.CurrentBalance,
                requiredAmount = ex.RequiredAmount
            });
        }
        catch (ConcurrencyConflictException ex)
        {
            // Conflito de concorrência otimista: xmin mudou entre SELECT e UPDATE.
            // 409 Conflict — o cliente pode tentar novamente.
            context.Response.StatusCode = 409;
            await WriteJsonAsync(context, new { title = "Conflict", detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = 400;
            await WriteJsonAsync(context, new { title = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await WriteJsonAsync(context, new { title = "An unexpected error occurred." });
        }
    }

    private static Task WriteJsonAsync(HttpContext context, object payload)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
