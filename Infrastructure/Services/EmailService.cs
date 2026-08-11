using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Nirvachak_AI.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// SMTP email service using MailKit. Configure via appsettings.json or environment variables:
///   Smtp:Host, Smtp:Port, Smtp:User, Smtp:Pass, Smtp:From, Smtp:FromName
/// Works with Gmail (port 587 STARTTLS or port 465 SSL), Outlook, SendGrid, Brevo, Mailgun etc.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var host      = _config["Smtp:Host"]     ?? _config["Smtp__Host"]     ?? _config["SMTP_HOST"];
        var portStr   = _config["Smtp:Port"]     ?? _config["Smtp__Port"]     ?? _config["SMTP_PORT"] ?? "587";
        var user      = _config["Smtp:User"]     ?? _config["Smtp__User"]     ?? _config["SMTP_USER"];
        var pass      = _config["Smtp:Pass"]     ?? _config["Smtp__Pass"]     ?? _config["SMTP_PASS"];
        var fromEmail = _config["Smtp:From"]     ?? _config["Smtp__From"]     ?? _config["SMTP_FROM"] ?? user;
        var fromName  = _config["Smtp:FromName"] ?? _config["Smtp__FromName"] ?? _config["SMTP_FROM_NAME"] ?? "Nirvachak AI";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            _logger.LogWarning("[Email] SMTP not configured. Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
            return;
        }

        var port = int.TryParse(portStr, out var p) ? p : 587;

        // Port 465 = implicit SSL, Port 587/25 = Auto (lets MailKit negotiate STARTTLS or plain)
        var socketOptions = port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.Auto;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail!));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("[Email] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }
}
