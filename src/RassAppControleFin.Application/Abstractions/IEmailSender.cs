using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Abstractions;

public sealed record EmailMessage(string To, string Template, Guid OperationId,
    IReadOnlyDictionary<string, string> Parameters);

public interface IEmailSender
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
