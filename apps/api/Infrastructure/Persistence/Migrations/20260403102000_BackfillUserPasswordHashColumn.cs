#pragma warning disable CS8981
using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260403102000_BackfillUserPasswordHashColumn")]
    public partial class BackfillUserPasswordHashColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE users
                ADD COLUMN IF NOT EXISTS "PasswordHash" character varying(512);
                """
            );

            migrationBuilder.Sql(
                """
                UPDATE users
                SET "PasswordHash" = ''
                WHERE "PasswordHash" IS NULL;
                """
            );

            migrationBuilder.Sql(
                """
                ALTER TABLE users
                ALTER COLUMN "PasswordHash" SET NOT NULL;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE users
                DROP COLUMN IF EXISTS "PasswordHash";
                """
            );
        }
    }
}
#pragma warning restore CS8981
