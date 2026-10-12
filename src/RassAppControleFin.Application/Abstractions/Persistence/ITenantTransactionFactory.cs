using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Abstractions.Persistence;

public interface ITenantTransactionFactory
{
    Task<Result<ITenantTransaction>> BeginAsync(CancellationToken cancellationToken, bool consistentSnapshot = false);
}

public interface ITenantTransaction : IAsyncDisposable
{
    Task<Result> CommitAsync(CancellationToken cancellationToken);
}
