using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SurveysController : ApiBaseController
{
    private readonly AppDbContext _db;

    public SurveysController(AppDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(List<SurveyListItem>), 200)]
    public async Task<IActionResult> GetSurveys()
    {
        var cId = GetConstituencyId();
        var query = _db.Surveys.Include(s => s.Responses).AsQueryable();
        if (cId.HasValue) query = query.Where(s => s.ConstituencyId == cId.Value);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SurveyListItem(
                s.Id, s.Title, s.Description, s.Category.ToString(),
                s.IsActive, s.Responses.Count, s.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Submit a response to a survey</summary>
    [HttpPost("{id}/respond")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> Respond(int id, [FromBody] SubmitSurveyResponseRequest req)
    {
        var survey = await _db.Surveys.FindAsync(id);
        if (survey == null) return NotFound(new ApiResult(false, "Survey not found."));
        if (!survey.IsActive) return BadRequest(new ApiResult(false, "This survey is no longer active."));

        _db.SurveyResponses.Add(new Domain.Entities.SurveyResponse
        {
            SurveyId        = id,
            RespondentName  = req.RespondentName,
            RespondentPhone = req.RespondentPhone,
            Ward            = req.Ward,
            BoothNumber     = req.BoothNumber,
            Rating          = req.Rating,
            Feedback        = req.Feedback,
            RespondedAt     = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Response submitted successfully."));
    }
}
