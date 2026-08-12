using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nirvachak_AI.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// Email service that supports both Resend and Mailjet HTTP APIs.
/// Both use port 443 (HTTPS) — works on Railway where all SMTP ports are blocked.
///
/// Provider selection (first configured wins):
///   1. Resend  — if Resend__ApiKey is set
///   2. Mailjet — if Mailjet__ApiKey + Mailjet__SecretKey are set
///
/// Railway environment variables for Resend:
///   Resend__ApiKey   = re_xxxxxxxxxxxxxxxxxxxx
///   Resend__From     = onboarding@yourdomain.com
///   Resend__FromName = Nirvachak AI
///
/// Railway environment variables for Mailjet:
///   Mailjet__ApiKey    = your-mailjet-api-key
///   Mailjet__SecretKey = your-mailjet-secret-key
///   Mailjet__From      = tapchauhan2001@gmail.com
///   Mailjet__FromName  = Nirvachak AI
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, IHttpClientFactory httpClientFactory,
        ILogger<SmtpEmailService> logger)
    {
        _config            = config;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var resendApiKey = _config["Resend:ApiKey"] ?? _config["Resend__ApiKey"];

        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            await SendViaResendAsync(toEmail, toName, subject, htmlBody, resendApiKey);
            return;
        }

        var mailjetApiKey    = _config["Mailjet:ApiKey"]    ?? _config["Mailjet__ApiKey"];
        var mailjetSecretKey = _config["Mailjet:SecretKey"] ?? _config["Mailjet__SecretKey"];
        var mailjetFrom      = _config["Mailjet:From"]      ?? _config["Mailjet__From"];

        if (!string.IsNullOrWhiteSpace(mailjetApiKey) && !string.IsNullOrWhiteSpace(mailjetSecretKey)
            && !string.IsNullOrWhiteSpace(mailjetFrom))
        {
            await SendViaMailjetAsync(toEmail, toName, subject, htmlBody,
                mailjetApiKey, mailjetSecretKey, mailjetFrom,
                _config["Mailjet:FromName"] ?? _config["Mailjet__FromName"] ?? "Nirvachak AI");
            return;
        }

        _logger.LogWarning("[Email] No email provider configured (Resend or Mailjet). Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
    }

    // ?? Resend ????????????????????????????????????????????????????????????????
    private async Task SendViaResendAsync(string toEmail, string toName, string subject,
        string htmlBody, string apiKey)
    {
        var from     = _config["Resend:From"]     ?? _config["Resend__From"]     ?? "noreply@nirvachak.ai";
        var fromName = _config["Resend:FromName"] ?? _config["Resend__FromName"] ?? "Nirvachak AI";

        var payload = new
        {
            from    = $"{fromName} <{from}>",
            to      = new[] { toEmail },
            subject = subject,
            html    = htmlBody
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var client = _httpClientFactory.CreateClient("resend");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.PostAsync("https://api.resend.com/emails", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Email] Resend API error {Status}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Resend API returned {response.StatusCode}: {body}");
            }

            _logger.LogInformation("[Email][Resend] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email][Resend] Failed to send '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }

    // ?? Mailjet ???????????????????????????????????????????????????????????????
    private async Task SendViaMailjetAsync(string toEmail, string toName, string subject,
        string htmlBody, string apiKey, string secretKey, string from, string fromName)
    {
        var payload = new
        {
            Messages = new[]
            {
                new
                {
                    From     = new { Email = from, Name = fromName },
                    To       = new[] { new { Email = toEmail, Name = toName } },
                    Subject  = subject,
                    HTMLPart = htmlBody
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var client      = _httpClientFactory.CreateClient("mailjet");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{secretKey}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var response = await client.PostAsync("https://api.mailjet.com/v3.1/send", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Email] Mailjet API error {Status}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Mailjet API returned {response.StatusCode}: {body}");
            }

            _logger.LogInformation("[Email][Mailjet] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email][Mailjet] Failed to send '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }
}
