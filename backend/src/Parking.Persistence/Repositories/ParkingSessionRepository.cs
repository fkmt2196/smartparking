using Microsoft.EntityFrameworkCore;
using Parking.Application.Common.Interfaces;
using Parking.Domain.Entities;
using Parking.Persistence.Contexts;

namespace Parking.Persistence.Repositories;

public class ParkingSessionRepository : IParkingSessionRepository
{
    private readonly ParkingDbContext _db;

    public ParkingSessionRepository(ParkingDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<ParkingSession?> FindBySessionIdAsync(string sessionId, CancellationToken ct = default)
        => _db.ParkingSessions
              .FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Chỉ tìm phiên có status "pending_slot" hoặc "parked".
    /// Phiên đã "exited" được phép tái sử dụng biển số (xe ra vào nhiều lần trong ngày).
    /// </remarks>
    public Task<ParkingSession?> FindActiveByCarPlateAsync(string carPlate, CancellationToken ct = default)
        => _db.ParkingSessions
              .FirstOrDefaultAsync(x =>
                  x.CarPlate == carPlate &&
                  (x.Status == ParkingSessionStatus.PendingSlot ||
                   x.Status == ParkingSessionStatus.Parked), ct);

    /// <inheritdoc />
    public async Task AddAsync(ParkingSession session, CancellationToken ct = default)
    {
        _db.ParkingSessions.Add(session);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ParkingSession session, CancellationToken ct = default)
    {
        _db.ParkingSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ParkingSession>> GetActiveByLotAsync(
        int parkingLotId, CancellationToken ct = default)
        => _db.ParkingSessions
              .AsNoTracking()
              .Where(x =>
                  x.ParkingLotId == parkingLotId &&
                  (x.Status == ParkingSessionStatus.PendingSlot ||
                   x.Status == ParkingSessionStatus.Parked))
              .OrderBy(x => x.EnteredAtUtc)
              .ToListAsync(ct)
              .ContinueWith<IReadOnlyList<ParkingSession>>(t => t.Result, ct);
}
