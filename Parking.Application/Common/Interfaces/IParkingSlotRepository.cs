using Parking.Domain.Entities;

namespace Parking.Application.Common.Interfaces;

public interface IParkingSlotRepository
{
    Task<ParkingSlot?> GetByIdAsync(int id);
    Task<IEnumerable<ParkingSlot>> GetAllAsync();
}
