using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2ProfileMetadataAndSlotSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicRules",
                schema: "organization",
                table: "Businesses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicStaffDisplayPolicy",
                schema: "organization",
                table: "Businesses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ShowNames");

            migrationBuilder.AddColumn<decimal>(
                name: "RatingAverage",
                schema: "organization",
                table: "Businesses",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                schema: "organization",
                table: "Businesses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                schema: "organization",
                table: "Businesses",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                schema: "organization",
                table: "Businesses",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxPublicSlots",
                schema: "organization",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotIntervalMinutes",
                schema: "organization",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessGalleryImages",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AltText = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessGalleryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessGalleryImages_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "organization",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessGalleryImages_BusinessId",
                schema: "organization",
                table: "BusinessGalleryImages",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessGalleryImages_TenantId_BusinessId_SortOrder",
                schema: "organization",
                table: "BusinessGalleryImages",
                columns: new[] { "TenantId", "BusinessId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessGalleryImages",
                schema: "organization");

            migrationBuilder.DropColumn(
                name: "PublicRules",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "PublicStaffDisplayPolicy",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "RatingAverage",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                schema: "organization",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "MaxPublicSlots",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "SlotIntervalMinutes",
                schema: "organization",
                table: "Branches");
        }
    }
}
