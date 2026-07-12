using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsModelDriftReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NoShowFixedAmount",
                schema: "payments",
                table: "PaymentPolicies",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NoShowPercentage",
                schema: "payments",
                table: "PaymentPolicies",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoShowFixedAmount",
                schema: "payments",
                table: "PaymentPolicies");

            migrationBuilder.DropColumn(
                name: "NoShowPercentage",
                schema: "payments",
                table: "PaymentPolicies");
        }
    }
}
