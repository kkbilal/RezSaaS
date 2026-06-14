using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4PaymentsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.CreateTable(
                name: "PaymentAuditLogEntries",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentIntents",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CustomerUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderCheckoutUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntents", x => x.Id);
                    table.CheckConstraint("CK_PaymentIntents_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_PaymentIntents_CurrencyCode", "length(\"CurrencyCode\") = 3");
                    table.CheckConstraint("CK_PaymentIntents_Target", "\"AppointmentRequestId\" IS NOT NULL OR \"AppointmentId\" IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "PaymentPolicies",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    HostedCheckoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPolicies", x => x.Id);
                    table.CheckConstraint("CK_PaymentPolicies_CurrencyCode", "length(\"CurrencyCode\") = 3");
                    table.CheckConstraint("CK_PaymentPolicies_FixedAmount", "\"FixedAmount\" IS NULL OR \"FixedAmount\" >= 0");
                    table.CheckConstraint("CK_PaymentPolicies_HostedCheckoutShape", "\"HostedCheckoutEnabled\" = FALSE OR length(\"ProviderKey\") > 0");
                    table.CheckConstraint("CK_PaymentPolicies_Percentage", "\"Percentage\" IS NULL OR (\"Percentage\" > 0 AND \"Percentage\" <= 100)");
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PayloadSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderEventId = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLogEntries_TenantId_OccurredAtUtc",
                schema: "payments",
                table: "PaymentAuditLogEntries",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_ProviderKey_ProviderReference",
                schema: "payments",
                table: "PaymentIntents",
                columns: new[] { "ProviderKey", "ProviderReference" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_AppointmentRequestId",
                schema: "payments",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "AppointmentRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_CustomerUserAccountId_Status",
                schema: "payments",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "CustomerUserAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPolicies_TenantId_BranchId_BranchScoped",
                schema: "payments",
                table: "PaymentPolicies",
                columns: new[] { "TenantId", "BranchId" },
                unique: true,
                filter: "\"BranchId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPolicies_TenantId_TenantWide",
                schema: "payments",
                table: "PaymentPolicies",
                column: "TenantId",
                unique: true,
                filter: "\"BranchId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_ProviderKey_ProviderEventId",
                schema: "payments",
                table: "PaymentWebhookEvents",
                columns: new[] { "ProviderKey", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_Status_ReceivedAtUtc",
                schema: "payments",
                table: "PaymentWebhookEvents",
                columns: new[] { "Status", "ReceivedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAuditLogEntries",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "PaymentIntents",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "PaymentPolicies",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents",
                schema: "payments");
        }
    }
}
