using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nirvachak_AI.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// Email service using Resend HTTP API (https://resend.com).
/// Uses port 443 (HTTPS) — works on Railway where all SMTP ports (25/465/587) are blocked.
/// Configure via Railway environment variables:
///   Resend__ApiKey   = re_xxxxxxxxxxxx   (your Resend API key)
///   Resend__From     = onboarding@resend.dev  (or your verified sender)
///   Resend__FromName = Nirvachak AI
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
        var apiKey   = _config["Resend:ApiKey"]   ?? _config["Resend__ApiKey"];
        var from     = _config["Resend:From"]     ?? _config["Resend__From"]     ?? "onboarding@resend.dev";
        var fromName = _config["Resend:FromName"] ?? _config["Resend__FromName"] ?? "Nirvachak AI";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("[Email] Resend API key not configured. Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
            return;
        }

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

            _logger.LogInformation("[Email] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }
}
