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
public class TransportController : ApiBaseController
{
    private readonly AppDbContext _db;
    public TransportController(AppDbContext db) => _db = db;

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles()
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);
        var q = _db.TransportVehicles.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(v => v.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<TransportVehicleItem>());
        var items = await q.OrderBy(v => v.BoothNumber)
            .Select(v => new TransportVehicleItem(
                v.Id, v.DriverName, v.DriverPhone,
                v.VehicleNumber, v.VehicleType, v.Capacity,
                v.BoothNumber, v.IsAvailable, v.Notes))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateTransportVehicleRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        _db.TransportVehicles.Add(new Domain.Entities.TransportVehicle
        {
            DriverName     = req.DriverName.Trim(),
            DriverPhone    = req.DriverPhone.Trim(),
            VehicleNumber  = req.VehicleNumber,
            VehicleType    = req.VehicleType,
            Capacity       = req.Capacity,
            BoothNumber    = req.BoothNumber,
            Notes          = req.Notes,
            ConstituencyId = cId,
            IsAvailable    = true,
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Vehicle added."));
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);
        var q = _db.VoterTransportRequests
            .Include(r => r.Voter).Include(r => r.Vehicle)
            .AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(r => r.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<TransportRequestItem>());

        var items = await q.OrderByDescending(r => r.RequestedAt)
            .Select(r => new TransportRequestItem(
                r.Id, r.VoterId, r.Voter!.Name, r.Voter.MobileNumber,
                r.VehicleId, r.Vehicle != null ? r.Vehicle.DriverName : null,
                r.Vehicle != null ? r.Vehicle.VehicleNumber : null,
                r.Status.ToString(), r.PickupAddress, r.RequestedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateTransportRequestRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        _db.VoterTransportRequests.Add(new Domain.Entities.VoterTransportRequest
        {
            VoterId            = req.VoterId,
            VehicleId          = req.VehicleId,
            PickupAddress      = req.PickupAddress,
            PickupNotes        = req.PickupNotes,
            Status             = TransportStatus.Pending,
            RequestedByUserId  = GetUserId(),
            ConstituencyId     = cId,
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Transport request created."));
    }

    [HttpPut("requests/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
    {
        var req = await _db.VoterTransportRequests.FindAsync(id);
        if (req is null) return NotFound();
        if (Enum.TryParse<TransportStatus>(status, out var st)) req.Status = st;
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Status updated."));
    }
}
