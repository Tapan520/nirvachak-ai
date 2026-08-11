using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nirvachak_AI.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// Email service using Mailjet HTTP API (https://mailjet.com).
/// Uses port 443 (HTTPS) — works on Railway where all SMTP ports are blocked.
/// Free tier: 200 emails/day, sends to ANY email address, no domain verification needed.
/// Configure via Railway environment variables:
///   Mailjet__ApiKey    = your-mailjet-api-key
///   Mailjet__SecretKey = your-mailjet-secret-key
///   Mailjet__From      = tapchauhan2001@gmail.com  (your verified sender email)
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
        var apiKey    = _config["Mailjet:ApiKey"]    ?? _config["Mailjet__ApiKey"];
        var secretKey = _config["Mailjet:SecretKey"] ?? _config["Mailjet__SecretKey"];
        var from      = _config["Mailjet:From"]      ?? _config["Mailjet__From"];
        var fromName  = _config["Mailjet:FromName"]  ?? _config["Mailjet__FromName"] ?? "Nirvachak AI";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("[Email] Mailjet not configured. Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
            return;
        }

        var payload = new
        {
            Messages = new[]
            {
                new
                {
                    From    = new { Email = from, Name = fromName },
                    To      = new[] { new { Email = toEmail, Name = toName } },
                    Subject = subject,
                    HTMLPart = htmlBody
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var client = _httpClientFactory.CreateClient("mailjet");
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

            _logger.LogInformation("[Email] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }
}
