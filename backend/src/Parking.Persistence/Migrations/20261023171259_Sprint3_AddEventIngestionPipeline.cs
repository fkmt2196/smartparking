using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Parking.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3_AddEventIngestionPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parking_event_logs",
                schema: "parking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parking_lot_id = table.Column<int>(type: "integer", nullable: false),
                    slot_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    car_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    processing_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "accepted"),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    event_timestamp_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_event_logs", x => x.id);
                    table.CheckConstraint("ck_event_logs_car_plate_not_empty", "length(trim(car_plate)) > 0");
                    table.CheckConstraint("ck_event_logs_event_id_not_empty", "length(trim(event_id)) > 0");
                    table.CheckConstraint("ck_event_logs_event_type", "event_type IN ('VehicleEntered', 'SensorTriggered', 'SlotParked', 'VehicleExited')");
                    table.CheckConstraint("ck_event_logs_processing_status", "processing_status IN ('accepted', 'rejected', 'duplicate')");
                },
                comment: "Nhật ký sự kiện IoT/Staff – append-only, không cập nhật hay xóa.");

            migrationBuilder.CreateTable(
                name: "parking_sessions",
                schema: "parking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    car_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parking_lot_id = table.Column<int>(type: "integer", nullable: false),
                    slot_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending_slot"),
                    warning_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entry_image_url = table.Column<string>(type: "text", nullable: true),
                    exit_image_url = table.Column<string>(type: "text", nullable: true),
                    fuel_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    car_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    car_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gate_staff_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    zone_staff_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entered_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    parked_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    exited_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_sessions", x => x.id);
                    table.CheckConstraint("ck_sessions_car_plate_not_empty", "length(trim(car_plate)) > 0");
                    table.CheckConstraint("ck_sessions_session_id_not_empty", "length(trim(session_id)) > 0");
                    table.CheckConstraint("ck_sessions_slot_code_format", "slot_code IS NULL OR slot_code ~ '^[A-Z][0-9]{1,2}-[0-9]{3}$'");
                    table.CheckConstraint("ck_sessions_status", "status IN ('pending_slot', 'parked', 'exited')");
                },
                comment: "Phiên gửi xe – mỗi lượt vào/ra tạo 1 phiên.");

            migrationBuilder.CreateIndex(
                name: "ix_event_logs_car_plate",
                schema: "parking",
                table: "parking_event_logs",
                column: "car_plate");

            migrationBuilder.CreateIndex(
                name: "ix_event_logs_lot_received",
                schema: "parking",
                table: "parking_event_logs",
                columns: new[] { "parking_lot_id", "received_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_event_logs_session_id",
                schema: "parking",
                table: "parking_event_logs",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ux_event_logs_event_id",
                schema: "parking",
                table: "parking_event_logs",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_lot_status",
                schema: "parking",
                table: "parking_sessions",
                columns: new[] { "parking_lot_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_plate_status",
                schema: "parking",
                table: "parking_sessions",
                columns: new[] { "car_plate", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_sessions_session_id",
                schema: "parking",
                table: "parking_sessions",
                column: "session_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_event_logs",
                schema: "parking");

            migrationBuilder.DropTable(
                name: "parking_sessions",
                schema: "parking");
        }
    }
}
