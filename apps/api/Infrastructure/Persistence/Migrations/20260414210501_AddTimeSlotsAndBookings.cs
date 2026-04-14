using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeSlotsAndBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "time_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    booked_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_slots", x => x.id);
                    table.CheckConstraint("chk_time_slots_booked_count_range", "\"booked_count\" >= 0 AND \"booked_count\" <= \"capacity\"");
                    table.CheckConstraint("chk_time_slots_capacity_positive", "\"capacity\" > 0");
                    table.CheckConstraint("chk_time_slots_time_range", "\"end_time\" > \"start_time\"");
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.CheckConstraint("chk_bookings_status", "\"status\" IN ('active', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_bookings_time_slots_slot_id",
                        column: x => x.slot_id,
                        principalTable: "time_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_slot_id",
                table: "bookings",
                column: "slot_id");

            migrationBuilder.CreateIndex(
                name: "ux_bookings_user_slot_active",
                table: "bookings",
                columns: new[] { "user_id", "slot_id" },
                unique: true,
                filter: "\"status\" = 'active'");

            migrationBuilder.Sql(
                """
                INSERT INTO time_slots (id, start_time, end_time, capacity, booked_count, created_at)
                VALUES
                    ('00000000-0000-0000-0000-000000000101', '2026-04-16 09:00:00+00', '2026-04-16 09:30:00+00', 3, 2, NOW()),
                    ('00000000-0000-0000-0000-000000000102', '2026-04-16 10:00:00+00', '2026-04-16 10:30:00+00', 2, 1, NOW()),
                    ('00000000-0000-0000-0000-000000000103', '2026-04-16 11:00:00+00', '2026-04-16 11:30:00+00', 1, 0, NOW())
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO bookings (id, user_id, slot_id, status, created_at)
                VALUES
                    ('00000000-0000-0000-0000-000000000201', '10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000101', 'active', NOW()),
                    ('00000000-0000-0000-0000-000000000202', '10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000101', 'active', NOW()),
                    ('00000000-0000-0000-0000-000000000203', '10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000102', 'active', NOW()),
                    ('00000000-0000-0000-0000-000000000204', '10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000103', 'cancelled', NOW())
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM bookings WHERE id IN (
                    '00000000-0000-0000-0000-000000000201',
                    '00000000-0000-0000-0000-000000000202',
                    '00000000-0000-0000-0000-000000000203',
                    '00000000-0000-0000-0000-000000000204'
                );
                DELETE FROM time_slots WHERE id IN (
                    '00000000-0000-0000-0000-000000000101',
                    '00000000-0000-0000-0000-000000000102',
                    '00000000-0000-0000-0000-000000000103'
                );
                """);

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "time_slots");
        }
    }
}
