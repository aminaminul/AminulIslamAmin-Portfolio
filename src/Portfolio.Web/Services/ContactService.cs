using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Services;

public record ContactSubmitResult(bool Saved, bool EmailSent, bool EmailConfigured, string Message);

public interface IContactService
{
    Task<ContactSubmitResult> SubmitAsync(string name, string email, string? subject, string message,
        string? ipAddress, CancellationToken ct = default);
}

public class ContactService : IContactService
{
    private readonly AppDbContext _db;
    private readonly ISiteSettingsService _settings;
    private readonly IEmailSender _email;
    private readonly ILogger<ContactService> _logger;

    public ContactService(AppDbContext db, ISiteSettingsService settings, IEmailSender email, ILogger<ContactService> logger)
    {
        _db = db;
        _settings = settings;
        _email = email;
        _logger = logger;
    }

    public async Task<ContactSubmitResult> SubmitAsync(string name, string email, string? subject, string message,
        string? ipAddress, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        if (!settings.ShowContactForm)
            return new ContactSubmitResult(false, false, false, "The contact form is currently closed.");

        var record = new ContactMessage
        {
            Name = name.Trim(),
            Email = email.Trim(),
            Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim(),
            Message = message.Trim(),
            IpHash = HashIp(ipAddress),
            EmailState = _email.IsConfigured ? EmailDeliveryState.NotAttempted : EmailDeliveryState.NotConfigured
        };

        _db.ContactMessages.Add(record);
        await _db.SaveChangesAsync(ct);

        if (!_email.IsConfigured)
        {
            _logger.LogWarning("Contact message {MessageId} stored, but email delivery is not configured.", record.Id);
            return new ContactSubmitResult(true, false, false,
                "Your message was saved. Email notifications are not configured on this site yet, so the owner will see it in the admin panel.");
        }

        var recipient = !string.IsNullOrWhiteSpace(settings.ContactFormRecipient)
            ? settings.ContactFormRecipient!
            : settings.ContactEmail ?? string.Empty;

        var encoder = HtmlEncoder.Default;
        var html = $"""
            <h2>New contact message</h2>
            <p><strong>Name:</strong> {encoder.Encode(record.Name)}<br>
            <strong>Email:</strong> {encoder.Encode(record.Email)}<br>
            <strong>Subject:</strong> {encoder.Encode(record.Subject ?? "(none)")}</p>
            <p style="white-space:pre-wrap">{encoder.Encode(record.Message)}</p>
            """;

        var result = await _email.SendAsync(
            recipient,
            $"Portfolio contact: {record.Subject ?? "New message"}",
            html,
            record.Email,
            ct);

        record.EmailState = result.Sent ? EmailDeliveryState.Sent : EmailDeliveryState.Failed;
        record.EmailError = result.Sent ? null : Trim(result.Error, 400);
        await _db.SaveChangesAsync(ct);

        return new ContactSubmitResult(true, result.Sent, true,
            result.Sent
                ? "Your message was sent. Thank you for getting in touch."
                : "Your message was saved, but the email notification could not be delivered. The owner will still see it in the admin panel.");
    }

    private static string HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return string.Empty;
        var normalized = IPAddress.TryParse(ip, out var address) ? address.ToString() : ip;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash)[..32];
    }

    private static string? Trim(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
