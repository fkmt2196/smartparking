using SmartParking.Domain.Entities;

namespace SmartParking.Domain.Interfaces
{
    public interface IParkingRepository
    {
        Task<ParkingSlot> GetSlotByIdAsync(string slotId);
    }
}
