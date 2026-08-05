using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

/// <summary>
/// Provides Exotel Click-to-Call and SMS capabilities scoped per constituency (tenant).
/// Config is stored in <see cref="ExotelConfig"/> and looked up by constituencyId.
/// </summary>
public interface IExotelService
{
    /// <summary>
    /// Initiates an Exotel click-to-call: Exotel first rings the agent's phone,
    /// then bridges to the voter's phone. Returns the Exotel call SID on success.
    /// </summary>
    Task<(bool success, string? callSid, string? error)> ClickToCallAsync(
        int constituencyId, string agentPhone, string voterPhone);

    /// <summary>Sends a transactional SMS to a voter via Exotel.</summary>
    Task<(bool success, string? smsSid, string? error)> SendSmsAsync(
        int constituencyId, string toPhone, string body);

    /// <summary>Returns true if Exotel is configured and enabled for this constituency.</summary>
    Task<bool> IsConfiguredAsync(int constituencyId);

    /// <summary>Returns the active config for the given constituency, or null.</summary>
    Task<ExotelConfig?> GetConfigAsync(int constituencyId);
}

public class ExotelService : IExotelService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ExotelService> _logger;
    private readonly IConfiguration _config;

    public ExotelService(AppDbContext db, IHttpClientFactory http,
        ILogger<ExotelService> logger, IConfiguration config)
    {
        _db     = db;
        _http   = http;
        _logger = logger;
        _config = config;
    }

    // ?? Click-to-Call ??????????????????????????????????????????????????????

    public async Task<(bool success, string? callSid, string? error)> ClickToCallAsync(
        int constituencyId, string agentPhone, string voterPhone)
    {
        var cfg = await GetConfigAsync(constituencyId);
        if (cfg == null)
            return (false, null, "Exotel is not configured for this constituency. Go to Admin ? Settings ? Exotel.");

        agentPhone = NormalizePhone(agentPhone);
        voterPhone = NormalizePhone(voterPhone);
        var exoPhone = NormalizePhone(cfg.ExoPhone);

        try
        {
            var client = BuildClient(cfg);
            var url    = BuildUrl(cfg, "Calls/connect.json");

            var appBaseUrl = _config["AppBaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var callbackUrl = string.IsNullOrEmpty(appBaseUrl)
                ? null
                : $"{appBaseUrl}/api/exotel/callback/call";

            var form = new Dictionary<string, string>
            {
                ["From"]      = agentPhone,   // agent's phone — Exotel dials this first
                ["To"]        = voterPhone,   // voter's phone — bridged after agent picks up
                ["CallerId"]  = exoPhone,     // your ExoPhone (shown as caller ID)
                ["Record"]    = "true",
                ["TimeLimit"] = "3600",
                ["TimeOut"]   = "30",
            };
            if (callbackUrl != null)
                form["StatusCallback"] = callbackUrl;

            var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Exotel ClickToCall failed [{Status}] for constituency {CId}: {Body}",
                    response.StatusCode, constituencyId, body);
                return (false, null, $"Exotel error {(int)response.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var sid = doc.RootElement.GetProperty("Call").GetProperty("Sid").GetString();

            _logger.LogInformation("Exotel call initiated Sid={Sid} Agent={Agent} Voter={Voter} Constituency={CId}",
                sid, agentPhone, voterPhone, constituencyId);

            return (true, sid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exotel ClickToCall exception for constituency {CId}", constituencyId);
            return (false, null, ex.Message);
        }
    }

    // ?? SMS ????????????????????????????????????????????????????????????????

    public async Task<(bool success, string? smsSid, string? error)> SendSmsAsync(
        int constituencyId, string toPhone, string body)
    {
        var cfg = await GetConfigAsync(constituencyId);
        if (cfg == null)
            return (false, null, "Exotel is not configured for this constituency.");

        toPhone = NormalizePhone(toPhone);

        try
        {
            var client = BuildClient(cfg);
            var url    = BuildUrl(cfg, "Sms/send.json");

            var from = !string.IsNullOrWhiteSpace(cfg.SmsSenderId)
                ? cfg.SmsSenderId
                : NormalizePhone(cfg.ExoPhone);

            var form = new Dictionary<string, string>
            {
                ["From"]     = from,
                ["To"]       = toPhone,
                ["Body"]     = body,
                ["Priority"] = "high",
            };

            var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
            var respBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Exotel SMS failed [{Status}] for constituency {CId}: {Body}",
                    response.StatusCode, constituencyId, respBody);
                return (false, null, $"Exotel SMS error {(int)response.StatusCode}: {respBody}");
            }

            using var doc = JsonDocument.Parse(respBody);
            var sid = doc.RootElement.GetProperty("SMSMessage").GetProperty("Sid").GetString();

            _logger.LogInformation("Exotel SMS sent Sid={Sid} To={To} Constituency={CId}",
                sid, toPhone, constituencyId);

            return (true, sid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exotel SMS exception for constituency {CId}", constituencyId);
            return (false, null, ex.Message);
        }
    }

    // ?? Helpers ????????????????????????????????????????????????????????????

    public async Task<bool> IsConfiguredAsync(int constituencyId)
        => await _db.ExotelConfigs.AnyAsync(e =>
            (e.ConstituencyId == constituencyId || e.ConstituencyId == null)
            && e.IsEnabled);

    public async Task<ExotelConfig?> GetConfigAsync(int constituencyId)
        => await _db.ExotelConfigs
            .Where(e => e.IsEnabled &&
                        (e.ConstituencyId == constituencyId || e.ConstituencyId == null))
            .OrderByDescending(e => e.ConstituencyId)   // constituency-specific takes priority over global
            .FirstOrDefaultAsync();

    private HttpClient BuildClient(ExotelConfig cfg)
    {
        var client = _http.CreateClient("exotel");
        var creds  = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{cfg.ApiKey}:{cfg.ApiToken}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", creds);
        return client;
    }

    private static string BuildUrl(ExotelConfig cfg, string endpoint)
    {
        var subdomain = cfg.Subdomain.Trim().TrimStart('h', 't', 'p', 's', ':', '/');
        return $"https://{subdomain}/v1/Accounts/{cfg.AccountSid}/{endpoint}";
    }

    /// <summary>
    /// Normalises an Indian phone number to the 0XXXXXXXXXX format required by Exotel.
    /// Handles: 9876543210, +919876543210, 919876543210, 09876543210
    /// </summary>
    public static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Already 0XXXXXXXXXX (11 digits starting with 0)
        if (digits.Length == 11 && digits.StartsWith('0'))
            return digits;

        // +91 or 91 prefix ? 12 digits
        if (digits.Length == 12 && digits.StartsWith("91"))
            return "0" + digits[2..];

        // Bare 10-digit number
        if (digits.Length == 10)
            return "0" + digits;

        return phone.Trim();
    }
}
