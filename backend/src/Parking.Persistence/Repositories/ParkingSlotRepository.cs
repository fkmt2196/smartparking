using Parking.Application.Common.Interfaces;
using Parking.Domain.Entities;

namespace Parking.Persistence.Repositories;

public class ParkingSlotRepository : IParkingSlotRepository
{
    // Hardcoded mock data (Xuống dưới cả lớp Persistence nhưng không cần gọi DB theo yêu cầu của Mentor)
    private static readonly List<ParkingSlot> MockParkingSlots = new()
    {
        new ParkingSlot { Id = 1, SlotNumber = "A-01", Floor = "Tầng 1", IsOccupied = false, HourlyRate = 25000 },
        new ParkingSlot { Id = 2, SlotNumber = "A-02", Floor = "Tầng 1", IsOccupied = true, HourlyRate = 25000 },
        new ParkingSlot { Id = 3, SlotNumber = "B-01", Floor = "Tầng 2", IsOccupied = false, HourlyRate = 30000 },
    };

    public Task<ParkingSlot?> GetByIdAsync(int id)
    {
        var slot = MockParkingSlots.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(slot);
    }

    public Task<IEnumerable<ParkingSlot>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<ParkingSlot>>(MockParkingSlots);
    }
}
