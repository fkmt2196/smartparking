namespace SmartParking.Domain.Entities
{
    public class ParkingSlot
    {
        public string SlotId { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
    }
}
