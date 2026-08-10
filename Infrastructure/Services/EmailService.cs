using System.Net;
using System.Net.Mail;

namespace Nirvachak_AI.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// SMTP email service. Configure via Railway environment variables:
///   SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM, SMTP_FROM_NAME
/// Works with Gmail, Outlook, SendGrid, Brevo, Mailgun etc.
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
        var host     = _config["Smtp__Host"] ?? _config["SMTP_HOST"];
        var portStr  = _config["Smtp__Port"] ?? _config["SMTP_PORT"] ?? "587";
        var user     = _config["Smtp__User"] ?? _config["SMTP_USER"];
        var pass     = _config["Smtp__Pass"] ?? _config["SMTP_PASS"];
        var fromEmail= _config["Smtp__From"] ?? _config["SMTP_FROM"] ?? user;
        var fromName = _config["Smtp__FromName"] ?? _config["SMTP_FROM_NAME"] ?? "Nirvachak AI";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            _logger.LogWarning("[Email] SMTP not configured. Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
            return;
        }

        var port    = int.TryParse(portStr, out var p) ? p : 587;
        var enableSsl = port != 25;

        using var client = new SmtpClient(host, port)
        {
            Credentials    = new NetworkCredential(user, pass),
            EnableSsl      = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout        = 15_000
        };

        using var message = new MailMessage
        {
            From       = new MailAddress(fromEmail!, fromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("[Email] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }
}
