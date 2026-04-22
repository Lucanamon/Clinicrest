using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientContactPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_email",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_sms",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "patients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_email",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "allow_sms",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "email",
                table: "patients");
        }
    }
}
