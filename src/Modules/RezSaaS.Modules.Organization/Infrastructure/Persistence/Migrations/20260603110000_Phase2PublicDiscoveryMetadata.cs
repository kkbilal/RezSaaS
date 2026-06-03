using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

#nullable disable

namespace RezSaaS.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrganizationDbContext))]
    [Migration("20260603110000_Phase2PublicDiscoveryMetadata")]
    public partial class Phase2PublicDiscoveryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_TenantId_NormalizedSlug",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "organization",
                table: "Businesses",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                schema: "organization",
                table: "Branches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "organization",
                table: "Branches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                schema: "organization",
                table: "Branches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCity",
                schema: "organization",
                table: "Branches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedDistrict",
                schema: "organization",
                table: "Branches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_NormalizedSlug",
                schema: "organization",
                table: "Businesses",
                column: "NormalizedSlug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_NormalizedCity_NormalizedDistrict",
                schema: "organization",
                table: "Branches",
                columns: new[] { "TenantId", "NormalizedCity", "NormalizedDistrict" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_NormalizedSlug",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TenantId_NormalizedCity_NormalizedDistrict",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "District",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "NormalizedCity",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "NormalizedDistrict",
                schema: "organization",
                table: "Branches");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_TenantId_NormalizedSlug",
                schema: "organization",
                table: "Businesses",
                columns: new[] { "TenantId", "NormalizedSlug" },
                unique: true);
        }
    }
}
