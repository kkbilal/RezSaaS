using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.TenantManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenantManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant_management");

            migrationBuilder.CreateTable(
                name: "TenantAuditLogEntries",
                schema: "tenant_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "tenant_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedSlug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SuspendedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                schema: "tenant_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                    table.CheckConstraint("CK_TenantMemberships_BusinessOwner_NotBranchScoped", "\"Role\" <> 'BusinessOwner' OR \"BranchId\" IS NULL");
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenant_management",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogEntries_TenantId_OccurredAtUtc",
                schema: "tenant_management",
                table: "TenantAuditLogEntries",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_Role",
                schema: "tenant_management",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_UserAccountId",
                schema: "tenant_management",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "UserAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_NormalizedSlug",
                schema: "tenant_management",
                table: "Tenants",
                column: "NormalizedSlug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantAuditLogEntries",
                schema: "tenant_management");

            migrationBuilder.DropTable(
                name: "TenantMemberships",
                schema: "tenant_management");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenant_management");
        }
    }
}
