using SmartParking.Domain.Entities;
using SmartParking.Domain.Interfaces;

namespace SmartParking.Infrastructure.Persistence
{
    public class MockParkingRepository : IParkingRepository
    {
        public Task<ParkingSlot> GetSlotByIdAsync(string slotId)
        {
            // Giả lập dữ liệu Database trong bộ nhớ
            var mockSlot = new ParkingSlot
            {
                SlotId = slotId,
                Zone = "Zone A",
                IsOccupied = true
            };

            return Task.FromResult(mockSlot);
        }
    }
}
