using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendNotificationJobRetryAndChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notification_jobs_status_scheduled_send_time",
                table: "notification_jobs");

            migrationBuilder.AddColumn<string>(
                name: "email_address",
                table: "notification_jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "notification_jobs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "notification_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE notification_jobs SET next_attempt_at = scheduled_send_time WHERE next_attempt_at IS NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE notification_jobs ALTER COLUMN next_attempt_at SET NOT NULL;");

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_at",
                table: "notification_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_jobs_status_next_attempt_at",
                table: "notification_jobs",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notification_jobs_status_next_attempt_at",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "email_address",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "message",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "notification_jobs");

            migrationBuilder.CreateIndex(
                name: "ix_notification_jobs_status_scheduled_send_time",
                table: "notification_jobs",
                columns: new[] { "status", "scheduled_send_time" });
        }
    }
}
