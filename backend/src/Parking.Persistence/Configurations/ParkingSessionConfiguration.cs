using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parking.Domain.Entities;

namespace Parking.Persistence.Configurations;

public class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
    public void Configure(EntityTypeBuilder<ParkingSession> builder)
    {
        builder.ToTable("parking_sessions", "parking", table =>
        {
            table.HasComment("Phiên gửi xe – mỗi lượt vào/ra tạo 1 phiên.");
            table.HasCheckConstraint("ck_sessions_session_id_not_empty",
                "length(trim(session_id)) > 0");
            table.HasCheckConstraint("ck_sessions_car_plate_not_empty",
                "length(trim(car_plate)) > 0");
            table.HasCheckConstraint("ck_sessions_status",
                "status IN ('pending_slot', 'parked', 'exited')");
            // SlotCode phải đúng format A1-012 hoặc null
            table.HasCheckConstraint("ck_sessions_slot_code_format",
                @"slot_code IS NULL OR slot_code ~ '^[A-Z][0-9]{1,2}-[0-9]{3}$'");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100)
            .IsRequired();

        // SessionId là duy nhất – mỗi phiên chỉ tồn tại 1 lần
        builder.HasIndex(x => x.SessionId)
            .IsUnique()
            .HasDatabaseName("ux_sessions_session_id");

        builder.Property(x => x.CarPlate)
            .HasColumnName("car_plate")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ParkingLotId)
            .HasColumnName("parking_lot_id")
            .IsRequired();

        builder.Property(x => x.SlotCode)
            .HasColumnName("slot_code")
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue(ParkingSessionStatus.PendingSlot)
            .IsRequired();

        builder.Property(x => x.WarningCode)
            .HasColumnName("warning_code")
            .HasMaxLength(50);

        builder.Property(x => x.EntryImageUrl)
            .HasColumnName("entry_image_url")
            .HasColumnType("text");

        builder.Property(x => x.ExitImageUrl)
            .HasColumnName("exit_image_url")
            .HasColumnType("text");

        builder.Property(x => x.FuelType)
            .HasColumnName("fuel_type")
            .HasMaxLength(30);

        builder.Property(x => x.CarColor)
            .HasColumnName("car_color")
            .HasMaxLength(50);

        builder.Property(x => x.CarModel)
            .HasColumnName("car_model")
            .HasMaxLength(100);

        builder.Property(x => x.GateStaffId)
            .HasColumnName("gate_staff_id")
            .HasMaxLength(100);

        builder.Property(x => x.ZoneStaffId)
            .HasColumnName("zone_staff_id")
            .HasMaxLength(100);

        builder.Property(x => x.EnteredAtUtc)
            .HasColumnName("entered_at_utc")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.ParkedAtUtc)
            .HasColumnName("parked_at_utc")
            .HasColumnType("timestamptz");

        builder.Property(x => x.ExitedAtUtc)
            .HasColumnName("exited_at_utc")
            .HasColumnType("timestamptz");

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

        // Index chính: tìm phiên mở theo biển số (chặn check-in trùng)
        builder.HasIndex(x => new { x.CarPlate, x.Status })
            .HasDatabaseName("ix_sessions_plate_status");

        // Index cho màn hình điều phối: lọc phiên đang chờ gán slot
        builder.HasIndex(x => new { x.ParkingLotId, x.Status })
            .HasDatabaseName("ix_sessions_lot_status");
    }
}
