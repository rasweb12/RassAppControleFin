using MediatR;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.System;

public sealed record FoundationInfo(string Name, string Version, string Stage);
public sealed record GetSystemStatusQuery : IRequest<Result<FoundationInfo>>;

public sealed class GetSystemStatusHandler : IRequestHandler<GetSystemStatusQuery, Result<FoundationInfo>>
{
    public Task<Result<FoundationInfo>> Handle(GetSystemStatusQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Result<FoundationInfo>.Success(
            new("RassAppControleFin", "1.0.0-alpha.1", "Foundation")));
    }
}
