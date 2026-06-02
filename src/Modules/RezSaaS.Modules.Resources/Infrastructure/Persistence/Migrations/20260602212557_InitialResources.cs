using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "resources");

            migrationBuilder.CreateTable(
                name: "ResourceBlocks",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceBlocks", x => x.Id);
                    table.CheckConstraint("CK_ResourceBlocks_EndAfterStart", "\"EndUtc\" > \"StartUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResourceTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBlocks_TenantId_ResourceId_StartUtc",
                schema: "resources",
                table: "ResourceBlocks",
                columns: new[] { "TenantId", "ResourceId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_BranchId",
                schema: "resources",
                table: "Resources",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_ResourceTypeId",
                schema: "resources",
                table: "Resources",
                columns: new[] { "TenantId", "ResourceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_TenantId_NormalizedKey",
                schema: "resources",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "NormalizedKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceBlocks",
                schema: "resources");

            migrationBuilder.DropTable(
                name: "Resources",
                schema: "resources");

            migrationBuilder.DropTable(
                name: "ResourceTypes",
                schema: "resources");
        }
    }
}
