using Microsoft.EntityFrameworkCore;
using Parking.Application.Common.Interfaces;
using Parking.Domain.Entities;
using Parking.Persistence.Contexts;

namespace Parking.Persistence.Repositories;

public class ParkingEventLogRepository : IParkingEventLogRepository
{
    private readonly ParkingDbContext _db;

    public ParkingEventLogRepository(ParkingDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<ParkingEventLog?> FindByEventIdAsync(string eventId, CancellationToken ct = default)
        => _db.ParkingEventLogs
              .AsNoTracking()
              .FirstOrDefaultAsync(x => x.EventId == eventId, ct);

    /// <inheritdoc />
    public async Task AddAsync(ParkingEventLog log, CancellationToken ct = default)
    {
        _db.ParkingEventLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ParkingEventLog>> GetRecentByLotAsync(
        int parkingLotId, int limit = 100, CancellationToken ct = default)
        => _db.ParkingEventLogs
              .AsNoTracking()
              .Where(x => x.ParkingLotId == parkingLotId)
              .OrderByDescending(x => x.ReceivedAtUtc)
              .Take(limit)
              .ToListAsync(ct)
              .ContinueWith<IReadOnlyList<ParkingEventLog>>(t => t.Result, ct);
}
