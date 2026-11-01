using Npgsql;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Identity;

/// <summary>
/// Uses the separate, least-privilege platform role. It never falls back to the
/// financial runtime connection, so a SQL injection scoped to the runtime cannot
/// mutate global assignments.
/// </summary>
public sealed class IsolatedPlatformAdministrationService(
    IPlatformAuthorizationService authorization,
    PlatformAdministrationDataSource administration) : IPlatformAdministrationService
{
    private static readonly Error Forbidden = new("PLATFORM_FORBIDDEN", "Permissao de plataforma insuficiente.", ErrorType.Forbidden);
    private static readonly Error Conflict = new("PLATFORM_ROLE_CONFLICT", "Operacao administrativa recusada.", ErrorType.Conflict);
    private static readonly Error Unavailable = new("PLATFORM_ADMIN_UNAVAILABLE", "Administracao de plataforma indisponivel.", ErrorType.Infrastructure);

    public async Task<Result<PlatformDashboard>> DashboardAsync(string actor, CancellationToken ct)
    {
        await using var scope = await OpenAuthorizedAsync(actor, ct);
        if (scope is null) return Fail<PlatformDashboard>();
        await using var command = new NpgsqlCommand("SELECT * FROM app.platform_dashboard()", scope.Connection, scope.Transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Result<PlatformDashboard>.Success(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2)));
    }

    public async Task<Result<IReadOnlyList<PlatformRoleView>>> RolesAsync(string actor, string userId, CancellationToken ct)
    {
        await using var scope = await OpenAuthorizedAsync(actor, ct);
        if (scope is null) return Fail<IReadOnlyList<PlatformRoleView>>();
        await using var command = new NpgsqlCommand("SELECT * FROM app.platform_roles(@userId)", scope.Connection, scope.Transaction);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var values = new List<PlatformRoleView>();
        while (await reader.ReadAsync(ct)) values.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3),
            reader.GetString(4), reader.GetFieldValue<DateTimeOffset>(5), reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7), reader.GetString(8)));
        return Result<IReadOnlyList<PlatformRoleView>>.Success(values);
    }

    public async Task<Result<PlatformRoleView>> GrantSuperAdminAsync(string actor, string targetUserId, string reason, CancellationToken ct)
    {
        if (actor == targetUserId) return Result<PlatformRoleView>.Failure(Conflict);
        await using var scope = await OpenAuthorizedAsync(actor, ct);
        if (scope is null) return Fail<PlatformRoleView>();
        var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        await using var command = new NpgsqlCommand("SELECT * FROM app.platform_grant_superadmin(@id,@auditId,@actor,@userId,@reason,@correlation)", scope.Connection, scope.Transaction);
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("userId", targetUserId); command.Parameters.AddWithValue("actor", actor);
        command.Parameters.AddWithValue("auditId", Guid.NewGuid()); command.Parameters.AddWithValue("reason", reason.Trim()); command.Parameters.AddWithValue("correlation", Guid.NewGuid().ToString());
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return Result<PlatformRoleView>.Failure(Conflict);
        now = reader.GetFieldValue<DateTimeOffset>(1);
        await scope.Transaction.CommitAsync(ct);
        return Result<PlatformRoleView>.Success(new(id, targetUserId, "SuperAdmin", true, actor, now, null, null, reason.Trim()));
    }

    public async Task<Result> RevokeSuperAdminAsync(string actor, string targetUserId, Guid assignmentId, string reason, CancellationToken ct)
    {
        await using var scope = await OpenAuthorizedAsync(actor, ct);
        if (scope is null) return administration.IsAvailable ? Result.Failure(Forbidden) : Result.Failure(Unavailable);
        await using var command = new NpgsqlCommand("SELECT app.platform_revoke_superadmin(@auditId,@actor,@userId,@id,@reason,@correlation)", scope.Connection, scope.Transaction);
        command.Parameters.AddWithValue("auditId", Guid.NewGuid()); command.Parameters.AddWithValue("actor", actor); command.Parameters.AddWithValue("userId", targetUserId);
        command.Parameters.AddWithValue("id", assignmentId); command.Parameters.AddWithValue("reason", reason.Trim()); command.Parameters.AddWithValue("correlation", Guid.NewGuid().ToString());
        if (await command.ExecuteScalarAsync(ct) is not true) return Result.Failure(Conflict);
        await scope.Transaction.CommitAsync(ct);
        return Result.Success();
    }

    private async Task<Scope?> OpenAuthorizedAsync(string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor) || !administration.IsAvailable) return null;
        if (!(await authorization.PermissionsAsync(actor, ct)).Contains(PlatformPermissions.RolesManage)) return null;
        var connection = await administration.OpenConnectionAsync(ct);
        return new Scope(connection, await connection.BeginTransactionAsync(ct));
    }

    private Result<T> Fail<T>() => Result<T>.Failure(administration.IsAvailable ? Forbidden : Unavailable);
    private sealed class Scope(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        public NpgsqlConnection Connection { get; } = connection;
        public NpgsqlTransaction Transaction { get; } = transaction;
        public async ValueTask DisposeAsync() { await Transaction.DisposeAsync(); await Connection.DisposeAsync(); }
    }
}
