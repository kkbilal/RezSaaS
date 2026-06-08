using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Booking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3AppointmentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessNote",
                schema: "booking",
                table: "Appointments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BusinessNoteUpdatedAtUtc",
                schema: "booking",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessNoteUpdatedByUserAccountId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "booking",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAtUtc",
                schema: "booking",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserAccountId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                schema: "booking",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByUserAccountId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNote",
                schema: "booking",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoShowAtUtc",
                schema: "booking",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NoShowByUserAccountId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoShowReason",
                schema: "booking",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RebookReason",
                schema: "booking",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RebookedAtUtc",
                schema: "booking",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RebookedByUserAccountId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RebookedFromAppointmentId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RebookedToAppointmentId",
                schema: "booking",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_RebookedFromAppointmentId",
                schema: "booking",
                table: "Appointments",
                columns: new[] { "TenantId", "RebookedFromAppointmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_RebookedToAppointmentId",
                schema: "booking",
                table: "Appointments",
                columns: new[] { "TenantId", "RebookedToAppointmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_TenantId_RebookedFromAppointmentId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TenantId_RebookedToAppointmentId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BusinessNote",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BusinessNoteUpdatedAtUtc",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BusinessNoteUpdatedByUserAccountId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancelledByUserAccountId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CompletedByUserAccountId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CompletionNote",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "NoShowAtUtc",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "NoShowByUserAccountId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "NoShowReason",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RebookReason",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RebookedAtUtc",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RebookedByUserAccountId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RebookedFromAppointmentId",
                schema: "booking",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RebookedToAppointmentId",
                schema: "booking",
                table: "Appointments");
        }
    }
}
