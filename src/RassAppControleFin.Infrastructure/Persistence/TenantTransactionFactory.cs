using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class TenantTransactionFactory(AppDbContext db, ITenantContext tenant,
    ICurrentUser user, IOptions<DatabaseOptions> options) : ITenantTransactionFactory
{
    public const string TelemetrySourceName = "RassAppControleFin.Persistence";
    private static readonly ActivitySource Activities = new(TelemetrySourceName);

    public async Task<Result<ITenantTransaction>> BeginAsync(CancellationToken cancellationToken, bool consistentSnapshot = false)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.Subject))
            return Result<ITenantTransaction>.Failure(new("IDENTITY_REQUIRED", "Autenticacao necessaria.", ErrorType.Unauthorized));
        if (!tenant.IsResolved || tenant.TenantId is null || tenant.TenantId == Guid.Empty)
            return Result<ITenantTransaction>.Failure(new("TENANT_REQUIRED", "Tenant autorizado necessario.", ErrorType.Forbidden));
        if (db.Database.CurrentTransaction is not null)
            return Result<ITenantTransaction>.Failure(new("TRANSACTION_ACTIVE", "Transacao ja ativa.", ErrorType.Conflict));

        using var activity = Activities.StartActivity("tenant.transaction.begin");
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await db.Database.BeginTransactionAsync(consistentSnapshot
                ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.ReadCommitted, cancellationToken);
            if (!await DatabaseSafety.VerifyAsync(db, options.Value.RuntimeRole, cancellationToken))
            {
                await transaction.DisposeAsync();
                return Result<ITenantTransaction>.Failure(DatabaseError());
            }
            var tenantId = tenant.TenantId.Value;
            var subject = user.Subject;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT set_config('app.tenant_id', {tenantId.ToString()}, true),
                       set_config('app.user_id', {subject}, true)
                """, cancellationToken);
            db.Authorize(tenantId, subject);
            return Result<ITenantTransaction>.Success(new TenantTransaction(db, transaction));
        }
        catch (OperationCanceledException)
        {
            if (transaction is not null) await transaction.DisposeAsync();
            throw;
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            if (transaction is not null) await transaction.DisposeAsync();
            activity?.SetStatus(ActivityStatusCode.Error, "database_unavailable");
            return Result<ITenantTransaction>.Failure(DatabaseError());
        }
    }

    private static Error DatabaseError() => new("DATABASE_UNAVAILABLE", "Persistencia indisponivel.", ErrorType.Infrastructure);

    private sealed class TenantTransaction(AppDbContext db, IDbContextTransaction transaction) : ITenantTransaction
    {
        private bool _completed;

        public async Task<Result> CommitAsync(CancellationToken cancellationToken)
        {
            if (_completed) return Result.Failure(new("TRANSACTION_COMPLETED", "Transacao encerrada.", ErrorType.Conflict));
            using var activity = Activities.StartActivity("tenant.transaction.commit");
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _completed = true;
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                await AbortAsync();
                return Result.Failure(new("VERSION_CONFLICT", "O recurso foi alterado.", ErrorType.Conflict));
            }
            catch (OperationCanceledException)
            {
                await AbortAsync();
                throw;
            }
            catch (Exception exception) when (exception is DbUpdateException or NpgsqlException or TimeoutException)
            {
                await AbortAsync();
                activity?.SetStatus(ActivityStatusCode.Error, "database_write_failed");
                var postgres = exception as PostgresException ?? exception.InnerException as PostgresException;
                return Result.Failure(postgres?.SqlState switch
                {
                    PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation
                        => new("PERSISTENCE_CONFLICT", "Conflito de persistencia.", ErrorType.Conflict),
                    PostgresErrorCodes.CheckViolation
                        => new("INVARIANT_VIOLATION", "Dados violam uma regra de integridade.", ErrorType.Domain),
                    PostgresErrorCodes.InsufficientPrivilege
                        => new("PERSISTENCE_FORBIDDEN", "Operacao nao autorizada.", ErrorType.Forbidden),
                    _ => DatabaseError()
                });
            }
        }

        private async Task AbortAsync()
        {
            _completed = true;
            try { await transaction.RollbackAsync(CancellationToken.None); }
            catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException or TimeoutException)
            {
                // Connection may already be broken; no retry or success claim after an ambiguous commit.
            }
            finally { db.ClearAuthorization(); }
        }

        public async ValueTask DisposeAsync()
        {
            _completed = true;
            try { await transaction.DisposeAsync(); }
            finally { db.ClearAuthorization(); }
        }
    }
}
