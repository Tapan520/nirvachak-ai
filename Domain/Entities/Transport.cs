using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class TransportVehicle
{
    public int Id { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DriverPhone { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
    public string? VehicleType { get; set; }   // Auto, Car, Van, Bus
    public int Capacity { get; set; }
    public int BoothNumber { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class VoterTransportRequest
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }
    public int? VehicleId { get; set; }
    public TransportVehicle? Vehicle { get; set; }
    public TransportStatus Status { get; set; } = TransportStatus.Pending;
    public string? PickupAddress { get; set; }
    public string? PickupNotes { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string? RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
}
