using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Parking.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitParkingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "parking");

            migrationBuilder.CreateTable(
                name: "parking_lots",
                schema: "parking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_lots", x => x.id);
                    table.CheckConstraint("ck_parking_lots_address_not_empty", "length(trim(address)) > 0");
                    table.CheckConstraint("ck_parking_lots_code_not_empty", "length(trim(code)) > 0");
                    table.CheckConstraint("ck_parking_lots_latitude", "latitude IS NULL OR (latitude >= -90 AND latitude <= 90)");
                    table.CheckConstraint("ck_parking_lots_longitude", "longitude IS NULL OR (longitude >= -180 AND longitude <= 180)");
                    table.CheckConstraint("ck_parking_lots_name_not_empty", "length(trim(name)) > 0");
                },
                comment: "Danh sách bãi đỗ xe");

            migrationBuilder.CreateTable(
                name: "parking_floors",
                schema: "parking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parking_lot_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_floors", x => x.id);
                    table.CheckConstraint("ck_parking_floors_code_not_empty", "length(trim(code)) > 0");
                    table.CheckConstraint("ck_parking_floors_display_order", "display_order >= 0");
                    table.CheckConstraint("ck_parking_floors_name_not_empty", "length(trim(name)) > 0");
                    table.ForeignKey(
                        name: "FK_parking_floors_parking_lots_parking_lot_id",
                        column: x => x.parking_lot_id,
                        principalSchema: "parking",
                        principalTable: "parking_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Tầng của bãi đỗ xe");

            migrationBuilder.CreateTable(
                name: "parking_slots",
                schema: "parking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parking_floor_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vehicle_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "available"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_slots", x => x.id);
                    table.CheckConstraint("ck_parking_slots_code_not_empty", "length(trim(code)) > 0");
                    table.CheckConstraint("ck_parking_slots_status", "status IN ('available', 'occupied', 'reserved', 'out_of_service')");
                    table.CheckConstraint("ck_parking_slots_vehicle_type", "vehicle_type IN ('car', 'moto', 'ev')");
                    table.ForeignKey(
                        name: "FK_parking_slots_parking_floors_parking_floor_id",
                        column: x => x.parking_floor_id,
                        principalSchema: "parking",
                        principalTable: "parking_floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Từng vị trí đỗ xe");

            migrationBuilder.CreateIndex(
                name: "ix_parking_floors_lot_display_order",
                schema: "parking",
                table: "parking_floors",
                columns: new[] { "parking_lot_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_parking_floors_lot_code",
                schema: "parking",
                table: "parking_floors",
                columns: new[] { "parking_lot_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parking_lots_is_active",
                schema: "parking",
                table: "parking_lots",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_parking_lots_code",
                schema: "parking",
                table: "parking_lots",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parking_slots_is_active",
                schema: "parking",
                table: "parking_slots",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_parking_slots_status",
                schema: "parking",
                table: "parking_slots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_parking_slots_floor_code",
                schema: "parking",
                table: "parking_slots",
                columns: new[] { "parking_floor_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_slots",
                schema: "parking");

            migrationBuilder.DropTable(
                name: "parking_floors",
                schema: "parking");

            migrationBuilder.DropTable(
                name: "parking_lots",
                schema: "parking");
        }
    }
}
