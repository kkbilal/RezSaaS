using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Booking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "AppointmentRequests",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomerUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRequests", x => x.Id);
                    table.CheckConstraint("CK_AppointmentRequests_EndAfterStart", "\"RequestedEndUtc\" > \"RequestedStartUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomerUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.CheckConstraint("CK_Appointments_EndAfterStart", "\"EndUtc\" > \"StartUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "AppointmentRequestLines",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ServiceNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRequestLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentRequestLines_AppointmentRequests_AppointmentRequ~",
                        column: x => x.AppointmentRequestId,
                        principalSchema: "booking",
                        principalTable: "AppointmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentLines",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ServiceNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentLines_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "booking",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentLines_AppointmentId",
                schema: "booking",
                table: "AppointmentLines",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequestLines_AppointmentRequestId",
                schema: "booking",
                table: "AppointmentRequestLines",
                column: "AppointmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_TenantId_BranchId_RequestedStartUtc",
                schema: "booking",
                table: "AppointmentRequests",
                columns: new[] { "TenantId", "BranchId", "RequestedStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_TenantId_CustomerUserAccountId_Status",
                schema: "booking",
                table: "AppointmentRequests",
                columns: new[] { "TenantId", "CustomerUserAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_BranchId_StartUtc",
                schema: "booking",
                table: "Appointments",
                columns: new[] { "TenantId", "BranchId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_ResourceId_StartUtc",
                schema: "booking",
                table: "Appointments",
                columns: new[] { "TenantId", "ResourceId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_StaffMemberId_StartUtc",
                schema: "booking",
                table: "Appointments",
                columns: new[] { "TenantId", "StaffMemberId", "StartUtc" });

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE booking."Appointments"
                ADD CONSTRAINT "EX_Appointments_ConfirmedStaff_NoOverlap"
                EXCLUDE USING gist
                (
                    "TenantId" WITH =,
                    "StaffMemberId" WITH =,
                    tstzrange("StartUtc", "EndUtc", '[)') WITH &&
                )
                WHERE ("Status" = 'Confirmed');
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE booking."Appointments"
                ADD CONSTRAINT "EX_Appointments_ConfirmedResource_NoOverlap"
                EXCLUDE USING gist
                (
                    "TenantId" WITH =,
                    "ResourceId" WITH =,
                    tstzrange("StartUtc", "EndUtc", '[)') WITH &&
                )
                WHERE ("Status" = 'Confirmed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentLines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "AppointmentRequestLines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "AppointmentRequests",
                schema: "booking");
        }
    }
}
