using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parking.Domain.Entities;

namespace Parking.Persistence.Configurations;

public class ParkingLotConfiguration : IEntityTypeConfiguration<ParkingLot>
{
    public void Configure(EntityTypeBuilder<ParkingLot> builder)
    {
        builder.ToTable("parking_lots", "parking", table =>
        {
            table.HasComment("Danh sách bãi đỗ xe");
            table.HasCheckConstraint("ck_parking_lots_code_not_empty", "length(trim(code)) > 0");
            table.HasCheckConstraint("ck_parking_lots_name_not_empty", "length(trim(name)) > 0");
            table.HasCheckConstraint("ck_parking_lots_address_not_empty", "length(trim(address)) > 0");
            table.HasCheckConstraint(
                "ck_parking_lots_latitude",
                "latitude IS NULL OR (latitude >= -90 AND latitude <= 90)");
            table.HasCheckConstraint(
                "ck_parking_lots_longitude",
                "longitude IS NULL OR (longitude >= -180 AND longitude <= 180)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasColumnName("address")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("numeric(9,6)");

        builder.Property(x => x.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("numeric(9,6)");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Ignore(x => x.AvailableSlots);
        builder.Ignore(x => x.HourlyRate);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_parking_lots_code");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_parking_lots_is_active");

        builder.HasMany(x => x.Floors)
            .WithOne(x => x.ParkingLot)
            .HasForeignKey(x => x.ParkingLotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
