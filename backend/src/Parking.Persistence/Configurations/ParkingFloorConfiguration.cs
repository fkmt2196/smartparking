using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parking.Domain.Entities;

namespace Parking.Persistence.Configurations;

public class ParkingFloorConfiguration : IEntityTypeConfiguration<ParkingFloor>
{
    public void Configure(EntityTypeBuilder<ParkingFloor> builder)
    {
        builder.ToTable("parking_floors", "parking", table =>
        {
            table.HasComment("Tầng của bãi đỗ xe");
            table.HasCheckConstraint("ck_parking_floors_code_not_empty", "length(trim(code)) > 0");
            table.HasCheckConstraint("ck_parking_floors_name_not_empty", "length(trim(name)) > 0");
            table.HasCheckConstraint("ck_parking_floors_display_order", "display_order >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.ParkingLotId)
            .HasColumnName("parking_lot_id")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(x => new { x.ParkingLotId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_parking_floors_lot_code");

        builder.HasIndex(x => new { x.ParkingLotId, x.DisplayOrder })
            .HasDatabaseName("ix_parking_floors_lot_display_order");

        builder.HasMany(x => x.Slots)
            .WithOne(x => x.ParkingFloor)
            .HasForeignKey(x => x.ParkingFloorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
