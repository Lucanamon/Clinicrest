using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260417143000_SyncPhoneFields")]
    public partial class SyncPhoneFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                ADD COLUMN IF NOT EXISTS phone_number character varying(32);

                ALTER TABLE appointments
                ADD COLUMN IF NOT EXISTS phone_number character varying(32);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE appointments
                DROP COLUMN IF EXISTS phone_number;
                """);
        }
    }
}
