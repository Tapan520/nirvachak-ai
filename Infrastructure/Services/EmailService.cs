using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nirvachak_AI.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// Email service using Brevo (https://brevo.com) HTTP API.
/// Uses port 443 (HTTPS) — works on Railway where all SMTP ports are blocked.
/// Free tier: 300 emails/day, sends to ANY email address, no domain verification needed.
/// Configure via Railway environment variables:
///   Brevo__ApiKey   = xkeysib-xxxx   (your Brevo API key)
///   Brevo__From     = tapchauhan2001@gmail.com  (your verified sender email in Brevo)
///   Brevo__FromName = Nirvachak AI
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
        var apiKey   = _config["Brevo:ApiKey"]   ?? _config["Brevo__ApiKey"];
        var from     = _config["Brevo:From"]     ?? _config["Brevo__From"];
        var fromName = _config["Brevo:FromName"] ?? _config["Brevo__FromName"] ?? "Nirvachak AI";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("[Email] Brevo not configured. Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
            return;
        }

        var payload = new
        {
            sender  = new { name = fromName, email = from },
            to      = new[] { new { name = toName, email = toEmail } },
            subject = subject,
            htmlContent = htmlBody
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var client = _httpClientFactory.CreateClient("brevo");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Email] Brevo API error {Status}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Brevo API returned {response.StatusCode}: {body}");
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
