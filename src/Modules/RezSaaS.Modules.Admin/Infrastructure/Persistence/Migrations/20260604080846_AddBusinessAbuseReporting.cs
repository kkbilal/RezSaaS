using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAbuseReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessAbuseReports",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ReasonCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ReportedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAbuseReports", x => x.Id);
                    table.CheckConstraint("CK_BusinessAbuseReports_NoSelfReport", "\"ReportedUserAccountId\" <> \"ReportedByUserAccountId\"");
                    table.CheckConstraint("CK_BusinessAbuseReports_ReviewShape", "(\"Status\" = 'PendingReview'\n    AND \"ReviewedAtUtc\" IS NULL\n    AND \"ReviewedByUserAccountId\" IS NULL\n    AND \"ReviewReason\" IS NULL)\nOR\n(\"Status\" IN ('Confirmed', 'Dismissed')\n    AND \"ReviewedAtUtc\" IS NOT NULL\n    AND \"ReviewedAtUtc\" >= \"CreatedAtUtc\"\n    AND \"ReviewedByUserAccountId\" IS NOT NULL\n    AND \"ReviewReason\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "UserStrikes",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SourceAbuseReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStrikes", x => x.Id);
                    table.CheckConstraint("CK_UserStrikes_ExpiryAfterIssue", "\"ExpiresAtUtc\" > \"IssuedAtUtc\"");
                    table.CheckConstraint("CK_UserStrikes_RevocationAfterIssue", "\"RevokedAtUtc\" IS NULL OR \"RevokedAtUtc\" >= \"IssuedAtUtc\"");
                    table.CheckConstraint("CK_UserStrikes_RevocationShape", "(\"RevokedAtUtc\" IS NULL\n    AND \"RevokedByUserAccountId\" IS NULL\n    AND \"RevocationReason\" IS NULL)\nOR\n(\"RevokedAtUtc\" IS NOT NULL\n    AND \"RevokedByUserAccountId\" IS NOT NULL\n    AND \"RevocationReason\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_UserStrikes_BusinessAbuseReports_SourceAbuseReportId",
                        column: x => x.SourceAbuseReportId,
                        principalSchema: "admin",
                        principalTable: "BusinessAbuseReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAbuseReports_ReportedUserAccountId_CreatedAtUtc",
                schema: "admin",
                table: "BusinessAbuseReports",
                columns: new[] { "ReportedUserAccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAbuseReports_TenantId_AppointmentRequestId",
                schema: "admin",
                table: "BusinessAbuseReports",
                columns: new[] { "TenantId", "AppointmentRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAbuseReports_TenantId_ReportedByUserAccountId_Creat~",
                schema: "admin",
                table: "BusinessAbuseReports",
                columns: new[] { "TenantId", "ReportedByUserAccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAbuseReports_TenantId_Status_CreatedAtUtc",
                schema: "admin",
                table: "BusinessAbuseReports",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserStrikes_SourceAbuseReportId",
                schema: "admin",
                table: "UserStrikes",
                column: "SourceAbuseReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStrikes_UserAccountId_ExpiresAtUtc",
                schema: "admin",
                table: "UserStrikes",
                columns: new[] { "UserAccountId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserStrikes_UserAccountId_IssuedAtUtc",
                schema: "admin",
                table: "UserStrikes",
                columns: new[] { "UserAccountId", "IssuedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserStrikes",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "BusinessAbuseReports",
                schema: "admin");
        }
    }
}
