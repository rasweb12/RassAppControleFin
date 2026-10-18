using System.Net.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RassAppControleFin.Application.Abstractions;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Email;

public sealed class BrevoSmtpEmailSender(IOptions<EmailOptions> options,
    ILogger<BrevoSmtpEmailSender> logger) : IEmailSender
{
    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!IsConfigured(config) || !IsValidAddress(message.To))
            return Result.Failure(new("EMAIL_UNAVAILABLE", "E-mail indisponivel.", ErrorType.Infrastructure));

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(config.From));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = Subject(message.Template);
        var body = Render(message);
        email.Body = new BodyBuilder { TextBody = body, HtmlBody = $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p>" }.ToMessageBody();

        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient { Timeout = 30000 };
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
            await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await client.SendAsync(email, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("Transactional email sent. OperationId={OperationId} Template={Template}",
                message.OperationId, message.Template);
            return Result.Success();
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException
            or IOException or OperationCanceledException)
        {
            logger.LogWarning("Transactional email unavailable. OperationId={OperationId} ExceptionType={ExceptionType}",
                message.OperationId, ex.GetType().Name);
            return Result.Failure(new("EMAIL_UNAVAILABLE", "E-mail indisponivel.", ErrorType.Infrastructure));
        }
    }

    private static bool IsConfigured(EmailOptions options) => options.UseSsl
        && string.Equals(options.SmtpHost, "smtp-relay.brevo.com", StringComparison.OrdinalIgnoreCase)
        && options.SmtpPort == 587 && !string.IsNullOrWhiteSpace(options.Username)
        && !string.IsNullOrWhiteSpace(options.Password) && IsValidAddress(options.From);

    private static bool IsValidAddress(string value)
    {
        try { _ = new MailAddress(value); return true; }
        catch (FormatException) { return false; }
    }

    private static string Subject(string template) => template switch
    {
        "notification" => "RASS Controle Financeiro: notificacao",
        _ => "RASS Controle Financeiro"
    };

    private static string Render(EmailMessage message) => message.Template switch
    {
        "notification" when message.Parameters.TryGetValue("message", out var content) => content,
        _ => "Ha uma atualizacao na sua conta RASS Controle Financeiro."
    };
}
