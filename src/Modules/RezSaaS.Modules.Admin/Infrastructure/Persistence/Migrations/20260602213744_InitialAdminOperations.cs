using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAdminOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "AbuseEvents",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbuseEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLogEntries",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSanctions",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSanctions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbuseEvents_TenantId_OccurredAtUtc",
                schema: "admin",
                table: "AbuseEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AbuseEvents_UserAccountId_OccurredAtUtc",
                schema: "admin",
                table: "AbuseEvents",
                columns: new[] { "UserAccountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogEntries_ActorUserAccountId_OccurredAtUtc",
                schema: "admin",
                table: "AdminAuditLogEntries",
                columns: new[] { "ActorUserAccountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_UserAccountId_StartsAtUtc",
                schema: "admin",
                table: "UserSanctions",
                columns: new[] { "UserAccountId", "StartsAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbuseEvents",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "AdminAuditLogEntries",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "UserSanctions",
                schema: "admin");
        }
    }
}
