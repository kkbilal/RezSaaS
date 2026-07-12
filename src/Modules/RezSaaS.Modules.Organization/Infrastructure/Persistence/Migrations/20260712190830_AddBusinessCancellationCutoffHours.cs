using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCancellationCutoffHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancellationCutoffHours",
                schema: "organization",
                table: "Businesses",
                type: "integer",
                nullable: false,
                // EF varsayilan olarak 0 uretmisti -- DEGISTIRILDI.
                // 0 = "iptal kurali yok" demek: mevcut TUM salonlar musterinin randevu
                // saatinden 5 dakika once bile iptal etmesine acik kalirdi (fail-open).
                // Domain varsayilani 2 saat; migration da onunla HIZALI olmali.
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationCutoffHours",
                schema: "organization",
                table: "Businesses");
        }
    }
}
