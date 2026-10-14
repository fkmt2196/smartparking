using System.Text.Json.Serialization;

namespace Parking.Domain.Entities;

public class ParkingSlot
{
    public int Id { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public decimal HourlyRate { get; set; }

    // Các field dưới đây dùng cho PostgreSQL, chưa trả về API JSON.
    [JsonIgnore]
    public int ParkingFloorId { get; set; }

    [JsonIgnore]
    public string Code { get; set; } = string.Empty;

    [JsonIgnore]
    public string VehicleType { get; set; } = string.Empty;

    [JsonIgnore]
    public string Status { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [JsonIgnore]
    public ParkingFloor? ParkingFloor { get; set; }
}
