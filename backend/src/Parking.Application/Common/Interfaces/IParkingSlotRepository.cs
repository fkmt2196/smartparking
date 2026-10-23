using Parking.Domain.Entities;

namespace Parking.Application.Common.Interfaces;

public interface IParkingSlotRepository
{
    Task<ParkingSlot?> GetByIdAsync(int id);
    Task<IEnumerable<ParkingSlot>> GetAllAsync();

    /// <summary>
    /// Tìm ô đậu xe theo mã ô (SlotCode) và bãi đỗ.
    /// Dùng để kiểm tra tương thích loại xe khi Staff gán slot.
    /// </summary>
    Task<ParkingSlot?> GetByCodeAsync(int parkingLotId, string slotCode, CancellationToken ct = default);
}
