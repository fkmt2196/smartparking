using Parking.Domain.Entities;

namespace Parking.Application.Common.Interfaces;

/// <summary>
/// Repository thao tác với bảng parking_event_logs.
/// Mọi thao tác đều là append-only – không cập nhật, không xóa bản ghi đã tạo.
/// </summary>
public interface IParkingEventLogRepository
{
    /// <summary>
    /// Tìm bản ghi log theo EventId (dùng để phát hiện trùng lặp – Idempotency Check).
    /// Trả về null nếu chưa tồn tại.
    /// </summary>
    Task<ParkingEventLog?> FindByEventIdAsync(string eventId, CancellationToken ct = default);

    /// <summary>Ghi một sự kiện mới vào nhật ký. Chỉ gọi sau khi FindByEventId trả null.</summary>
    Task AddAsync(ParkingEventLog log, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách sự kiện gần đây nhất theo bãi đỗ, phục vụ màn hình giám sát real-time.
    /// </summary>
    Task<IReadOnlyList<ParkingEventLog>> GetRecentByLotAsync(
        int parkingLotId, int limit = 100, CancellationToken ct = default);
}
