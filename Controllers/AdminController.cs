using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
           Roles = "Admin,SuperAdmin,CampaignManager")]
public class AdminController : ApiBaseController
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public AdminController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);

        IQueryable<AppUser> q = _db.Users.Include(u => u.Constituency)
            .OrderBy(u => u.FullName);

        if (!isSA && cId.HasValue)
            q = q.Where(u => u.ConstituencyId == cId && u.Role != UserRole.SuperAdmin);

        var items = await q.Select(u => new AdminUserItem(
            u.Id, u.FullName, u.Email, u.Role.ToString(),
            u.Constituency != null ? u.Constituency.Name : null,
            u.AssignedWard, u.IsActive))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPut("users/{id}/toggle")]
    public async Task<IActionResult> ToggleUser(string id)
    {
        var currentUserId = GetUserId();
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.Role == UserRole.SuperAdmin) return Forbid();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        return Ok(new ApiResult(true, $"User {(user.IsActive ? "enabled" : "disabled")}."));
    }
}
