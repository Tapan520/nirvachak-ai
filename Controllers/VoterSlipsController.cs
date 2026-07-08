using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VoterSlipsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public VoterSlipsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetSlips([FromQuery] int? booth, [FromQuery] int page = 1)
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);
        const int pageSize = 50;

        var q = _db.Voters.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(v => v.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<VoterSlipItem>());
        if (booth.HasValue) q = q.Where(v => v.BoothNumber == booth.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new VoterSlipItem(
                v.Id, v.VoterId, v.Name, v.NameLocal,
                v.BoothNumber, v.WardNumber, v.PannaNumber,
                v.SerialNumber, v.Age, v.Gender, v.Address))
            .ToListAsync();

        return Ok(new PagedResult<VoterSlipItem>(items, total, page, pageSize,
            (int)Math.Ceiling((double)total / pageSize)));
    }
}
