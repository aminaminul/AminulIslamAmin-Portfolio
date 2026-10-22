using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Portfolio.Web.Models.Options;

namespace Portfolio.Web.Services;

public record EmailSendResult(bool Configured, bool Sent, string? Error)
{
    public static EmailSendResult NotConfigured() => new(false, false, "Email delivery is not configured.");
    public static EmailSendResult Ok() => new(true, true, null);
    public static EmailSendResult Failed(string error) => new(true, false, error);
}

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, string? replyTo, CancellationToken ct = default);
}

/// <summary>SMTP delivery. Fails honestly when configuration is missing or the server rejects the message.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, string? replyTo, CancellationToken ct = default)
    {
        if (!IsConfigured) return EmailSendResult.NotConfigured();
        if (string.IsNullOrWhiteSpace(to)) return EmailSendResult.Failed("No recipient address is configured.");

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName ?? "Portfolio", _options.FromAddress!));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            if (!string.IsNullOrWhiteSpace(replyTo))
                message.ReplyTo.Add(MailboxAddress.Parse(replyTo));

            message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = StripHtml(htmlBody) }.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = _options.UseStartTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
            await client.ConnectAsync(_options.Host!, _options.Port, socketOptions, ct);
            if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrEmpty(_options.Password))
                await client.AuthenticateAsync(_options.UserName, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            return EmailSendResult.Ok();
        }
        catch (Exception ex)
        {
            // Log the failure without exposing credentials or the message body.
            _logger.LogError(ex, "Email delivery failed for a contact submission.");
            return EmailSendResult.Failed("The email provider could not deliver the notification.");
        }
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
}
