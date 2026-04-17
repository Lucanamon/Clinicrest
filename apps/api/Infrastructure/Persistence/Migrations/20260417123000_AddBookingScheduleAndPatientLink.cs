using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    public partial class AddBookingScheduleAndPatientLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                ADD COLUMN IF NOT EXISTS phone_number character varying(32);
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                ADD COLUMN IF NOT EXISTS patient_id uuid;
                ALTER TABLE bookings
                ADD COLUMN IF NOT EXISTS doctor_id uuid;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE bookings DROP CONSTRAINT IF EXISTS chk_bookings_status;
                ALTER TABLE bookings
                ADD CONSTRAINT chk_bookings_status CHECK ("status" IN ('ACTIVE', 'SCHEDULED', 'CANCELLED'));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE bookings DROP CONSTRAINT IF EXISTS chk_bookings_status;
                ALTER TABLE bookings
                ADD CONSTRAINT chk_bookings_status CHECK ("status" IN ('ACTIVE', 'CANCELLED'));
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                DROP COLUMN IF EXISTS patient_id;
                ALTER TABLE bookings
                DROP COLUMN IF EXISTS doctor_id;
                """);
        }
    }
}
