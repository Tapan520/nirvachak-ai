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
public class VolunteersController : ApiBaseController
{
    private readonly AppDbContext _db;

    public VolunteersController(AppDbContext db) => _db = db;

    /// <summary>Get all active volunteers</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<VolunteerListItem>), 200)]
    public async Task<IActionResult> GetVolunteers()
    {
        var cId = GetConstituencyId();
        IQueryable<Domain.Entities.Volunteer> query = _db.Volunteers.OrderBy(v => v.Name);
        if (cId.HasValue) query = query.Where(v => v.ConstituencyId == cId);

        var items = await query
            .Select(v => new VolunteerListItem(
                v.Id, v.Name, v.Phone, v.Task.ToString(),
                v.AssignedArea, v.AssignedBoothNumbers, v.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Create a new volunteer</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> Create([FromBody] CreateVolunteerRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        if (!Enum.TryParse<Domain.Enums.VolunteerTask>(req.Task, out var task))
            task = Domain.Enums.VolunteerTask.Other;

        _db.Volunteers.Add(new Domain.Entities.Volunteer
        {
            Name                 = req.Name.Trim(),
            Phone                = req.Phone.Trim(),
            Email                = req.Email,
            Address              = req.Address,
            Task                 = task,
            AssignedArea         = req.AssignedArea,
            AssignedBoothNumbers = req.AssignedBoothNumbers,
            Notes                = req.Notes,
            ConstituencyId       = cId,
            IsActive             = true
        });

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Volunteer added successfully."));
    }
}
