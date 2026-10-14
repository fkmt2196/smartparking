using Microsoft.EntityFrameworkCore;
using Parking.Domain.Entities;

namespace Parking.Persistence.Contexts;

public class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
    public DbSet<ParkingFloor> ParkingFloors => Set<ParkingFloor>();
    public DbSet<ParkingSlot> ParkingSlots => Set<ParkingSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("parking");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyUtcTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyUtcTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyUtcTimestamps()
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ParkingLot>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.UpdatedAtUtc = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ParkingSlot>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }
    }
}
