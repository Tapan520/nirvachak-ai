using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Controllers;

/// <summary>
/// Exotel integration endpoints:
///   POST /api/exotel/call          — click-to-call (agent ? voter)
///   POST /api/exotel/sms           — send SMS to a voter
///   POST /api/exotel/callback/call — Exotel status callback (webhook, anonymous)
///   GET  /api/exotel/status        — check if Exotel is configured for caller's constituency
/// </summary>
[ApiController]
[Route("api/exotel")]
[Authorize]
public class ExotelController : ControllerBase
{
    private readonly IExotelService _exotel;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ExotelController> _logger;

    public ExotelController(IExotelService exotel, AppDbContext db,
        UserManager<AppUser> userManager, ILogger<ExotelController> logger)
    {
        _exotel      = exotel;
        _db          = db;
        _userManager = userManager;
        _logger      = logger;
    }

    // ?? Click-to-Call ??????????????????????????????????????????????????????

    /// <summary>
    /// Initiates an Exotel click-to-call. Exotel first rings the calling agent's
    /// phone, then bridges to the voter's phone.
    /// Body: { "voterId": 123 }
    /// </summary>
    [HttpPost("call")]
    public async Task<IActionResult> ClickToCall([FromBody] ClickToCallRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var voter = await _db.Voters.FindAsync(req.VoterId);
        if (voter == null) return NotFound(new { error = "Voter not found." });

        if (string.IsNullOrWhiteSpace(voter.MobileNumber))
            return BadRequest(new { error = "Voter has no mobile number on record." });

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            return BadRequest(new { error = "Your profile has no phone number. Please update your profile." });

        var constituencyId = user.ConstituencyId ?? voter.ConstituencyId;

        var (success, callSid, error) =
            await _exotel.ClickToCallAsync(constituencyId, user.PhoneNumber, voter.MobileNumber);

        if (!success)
            return BadRequest(new { error });

        // Log the initiated call in PhoneCallLogs
        _db.PhoneCallLogs.Add(new PhoneCallLog
        {
            VoterId          = voter.Id,
            CalledByUserId   = user.Id,
            CalledByName     = user.FullName,
            CalledAt         = DateTime.UtcNow,
            Outcome          = CallOutcome.NoAnswer,   // updated later via callback or manual log
            ConstituencyId   = voter.ConstituencyId,
            Notes            = $"[Exotel] CallSid: {callSid}"
        });
        await _db.SaveChangesAsync();

        return Ok(new { callSid, message = "Call initiated. Your phone will ring shortly." });
    }

    // ?? Send SMS ???????????????????????????????????????????????????????????

    /// <summary>
    /// Sends an SMS to a voter via Exotel.
    /// Body: { "voterId": 123, "message": "Dear voter, ..." }
    /// </summary>
    [HttpPost("sms")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var voter = await _db.Voters.FindAsync(req.VoterId);
        if (voter == null) return NotFound(new { error = "Voter not found." });

        if (string.IsNullOrWhiteSpace(voter.MobileNumber))
            return BadRequest(new { error = "Voter has no mobile number on record." });

        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Message body cannot be empty." });

        var constituencyId = user.ConstituencyId ?? voter.ConstituencyId;

        var (success, smsSid, error) =
            await _exotel.SendSmsAsync(constituencyId, voter.MobileNumber, req.Message);

        if (!success)
            return BadRequest(new { error });

        return Ok(new { smsSid, message = "SMS sent successfully." });
    }

    // ?? Exotel Status Callback (Webhook) ???????????????????????????????????

    /// <summary>
    /// Receives Exotel's POST callback after a call ends.
    /// Exotel posts form data: CallSid, Status, Duration, RecordingUrl, etc.
    /// This endpoint is intentionally anonymous (Exotel doesn't send auth headers).
    /// </summary>
    [HttpPost("callback/call")]
    [AllowAnonymous]
    public async Task<IActionResult> CallCallback([FromForm] ExotelCallbackForm form)
    {
        _logger.LogInformation("Exotel callback: Sid={Sid} Status={Status} Duration={Duration}",
            form.CallSid, form.Status, form.Duration);

        // Find the most recent PhoneCallLog that references this CallSid
        var log = await _db.PhoneCallLogs
            .Where(l => l.Notes != null && l.Notes.Contains(form.CallSid ?? ""))
            .OrderByDescending(l => l.CalledAt)
            .FirstOrDefaultAsync();

        if (log != null)
        {
            log.DurationSeconds = form.Duration;
            log.Outcome = form.Status?.ToLower() switch
            {
                "completed" => CallOutcome.Talked,
                "no-answer" => CallOutcome.NoAnswer,
                "busy"      => CallOutcome.NoAnswer,
                "failed"    => CallOutcome.NoAnswer,
                _           => CallOutcome.NoAnswer
            };
            // Append recording URL to notes if provided
            if (!string.IsNullOrWhiteSpace(form.RecordingUrl))
                log.Notes += $" | Recording: {form.RecordingUrl}";

            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    // ?? Status Check ???????????????????????????????????????????????????????

    /// <summary>Returns whether Exotel is configured for the current user's constituency.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!user.ConstituencyId.HasValue)
            return Ok(new { isConfigured = false });

        var isConfigured = await _exotel.IsConfiguredAsync(user.ConstituencyId.Value);
        return Ok(new { isConfigured });
    }
}

// ?? Request / Callback DTOs ????????????????????????????????????????????????

public record ClickToCallRequest(int VoterId);
public record SendSmsRequest(int VoterId, string Message);

public class ExotelCallbackForm
{
    [FromForm(Name = "CallSid")]    public string? CallSid      { get; set; }
    [FromForm(Name = "Status")]     public string? Status       { get; set; }
    [FromForm(Name = "Duration")]   public int     Duration     { get; set; }
    [FromForm(Name = "RecordingUrl")] public string? RecordingUrl { get; set; }
    [FromForm(Name = "From")]       public string? From         { get; set; }
    [FromForm(Name = "To")]         public string? To           { get; set; }
}
