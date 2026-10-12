using Microsoft.EntityFrameworkCore;

namespace RassAppControleFin.Infrastructure.Persistence;

public static class RuntimeRoleBootstrap
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        using var stream = typeof(RuntimeRoleBootstrap).Assembly
            .GetManifestResourceStream("RassAppControleFin.bootstrap-runtime.sql")
            ?? throw new InvalidOperationException("Runtime grants resource missing.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
