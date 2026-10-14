using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parking.Domain;
using Parking.Domain.Entities;

namespace Parking.Persistence.Configurations;

public class ParkingSlotConfiguration : IEntityTypeConfiguration<ParkingSlot>
{
    public void Configure(EntityTypeBuilder<ParkingSlot> builder)
    {
        var vehicleTypes = string.Join(", ", ParkingDataRules.VehicleTypes.Select(value => $"'{value}'"));
        var slotStatuses = string.Join(", ", ParkingDataRules.SlotStatuses.Select(value => $"'{value}'"));

        builder.ToTable("parking_slots", "parking", table =>
        {
            table.HasComment("Từng vị trí đỗ xe");
            table.HasCheckConstraint("ck_parking_slots_code_not_empty", "length(trim(code)) > 0");
            table.HasCheckConstraint("ck_parking_slots_vehicle_type", $"vehicle_type IN ({vehicleTypes})");
            table.HasCheckConstraint("ck_parking_slots_status", $"status IN ({slotStatuses})");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.ParkingFloorId)
            .HasColumnName("parking_floor_id")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.VehicleType)
            .HasColumnName("vehicle_type")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue("available")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Ignore(x => x.SlotNumber);
        builder.Ignore(x => x.Floor);
        builder.Ignore(x => x.IsOccupied);
        builder.Ignore(x => x.HourlyRate);

        builder.HasIndex(x => new { x.ParkingFloorId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_parking_slots_floor_code");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_parking_slots_status");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_parking_slots_is_active");
    }
}
