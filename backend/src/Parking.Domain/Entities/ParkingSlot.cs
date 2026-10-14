namespace Parking.Domain.Entities;

public class ParkingSlot
{
    public int Id { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public decimal HourlyRate { get; set; }
}
