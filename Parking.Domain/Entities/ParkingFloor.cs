namespace Parking.Domain.Entities;

public class ParkingFloor
{
    public int Id { get; set; }
    public int ParkingLotId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ParkingLot? ParkingLot { get; set; }
    public ICollection<ParkingSlot> Slots { get; set; } = new List<ParkingSlot>();
}
