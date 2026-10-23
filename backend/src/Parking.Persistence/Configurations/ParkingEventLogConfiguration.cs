using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parking.Domain.Entities;

namespace Parking.Persistence.Configurations;

public class ParkingEventLogConfiguration : IEntityTypeConfiguration<ParkingEventLog>
{
    public void Configure(EntityTypeBuilder<ParkingEventLog> builder)
    {
        builder.ToTable("parking_event_logs", "parking", table =>
        {
            table.HasComment("Nhật ký sự kiện IoT/Staff – append-only, không cập nhật hay xóa.");
            table.HasCheckConstraint("ck_event_logs_event_id_not_empty",
                "length(trim(event_id)) > 0");
            table.HasCheckConstraint("ck_event_logs_car_plate_not_empty",
                "length(trim(car_plate)) > 0");
            table.HasCheckConstraint("ck_event_logs_event_type",
                "event_type IN ('VehicleEntered', 'SensorTriggered', 'SlotParked', 'VehicleExited')");
            table.HasCheckConstraint("ck_event_logs_processing_status",
                "processing_status IN ('accepted', 'rejected', 'duplicate')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        // Unique constraint tại DB – tầng phòng thủ cuối cho Idempotency
        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.EventId)
            .IsUnique()
            .HasDatabaseName("ux_event_logs_event_id");

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100);

        builder.Property(x => x.ParkingLotId)
            .HasColumnName("parking_lot_id")
            .IsRequired();

        builder.Property(x => x.SlotCode)
            .HasColumnName("slot_code")
            .HasMaxLength(20);

        builder.Property(x => x.CarPlate)
            .HasColumnName("car_plate")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TriggeredBy)
            .HasColumnName("triggered_by")
            .HasMaxLength(100);

        builder.Property(x => x.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasMaxLength(20)
            .HasDefaultValue("accepted")
            .IsRequired();

        builder.Property(x => x.RejectReason)
            .HasColumnName("reject_reason")
            .HasColumnType("text");

        builder.Property(x => x.EventTimestampUtc)
            .HasColumnName("event_timestamp_utc")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .HasColumnName("received_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Index để query log nhanh theo bãi + thời gian (màn hình giám sát real-time)
        builder.HasIndex(x => new { x.ParkingLotId, x.ReceivedAtUtc })
            .HasDatabaseName("ix_event_logs_lot_received");

        // Index tra theo biển số xe (debug + tra soát tranh chấp)
        builder.HasIndex(x => x.CarPlate)
            .HasDatabaseName("ix_event_logs_car_plate");

        // Index tra theo phiên
        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("ix_event_logs_session_id");
    }
}
