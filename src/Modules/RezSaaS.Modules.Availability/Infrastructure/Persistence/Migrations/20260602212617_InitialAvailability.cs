using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Availability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "availability");

            migrationBuilder.CreateTable(
                name: "BranchWorkingHours",
                schema: "availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchWorkingHours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffUnavailableTimes",
                schema: "availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffUnavailableTimes", x => x.Id);
                    table.CheckConstraint("CK_StaffUnavailableTimes_EndAfterStart", "\"EndUtc\" > \"StartUtc\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchWorkingHours_TenantId_BranchId_DayOfWeek",
                schema: "availability",
                table: "BranchWorkingHours",
                columns: new[] { "TenantId", "BranchId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffUnavailableTimes_TenantId_StaffMemberId_StartUtc",
                schema: "availability",
                table: "StaffUnavailableTimes",
                columns: new[] { "TenantId", "StaffMemberId", "StartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchWorkingHours",
                schema: "availability");

            migrationBuilder.DropTable(
                name: "StaffUnavailableTimes",
                schema: "availability");
        }
    }
}
