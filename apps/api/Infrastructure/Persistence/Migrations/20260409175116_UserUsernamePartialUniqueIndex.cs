using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserUsernamePartialUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_username_unique",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_username_active_unique",
                table: "users",
                column: "Username",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_username_active_unique",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_username_unique",
                table: "users",
                column: "Username",
                unique: true);
        }
    }
}
