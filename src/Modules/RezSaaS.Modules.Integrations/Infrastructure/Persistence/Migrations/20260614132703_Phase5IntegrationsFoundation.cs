using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Integrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5IntegrationsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "integrations");

            migrationBuilder.CreateTable(
                name: "IntegrationApiClients",
                schema: "integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    KeyHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScopeSet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationApiClients", x => x.Id);
                    table.CheckConstraint("CK_IntegrationApiClients_KeyHashSha256", "length(\"KeyHashSha256\") = 64");
                    table.CheckConstraint("CK_IntegrationApiClients_RevocationShape", "(\"Status\" = 'Active'\n    AND \"RevokedByUserAccountId\" IS NULL\n    AND \"RevokedAtUtc\" IS NULL\n    AND length(\"RevocationReason\") = 0)\nOR\n(\"Status\" = 'Revoked'\n    AND \"RevokedByUserAccountId\" IS NOT NULL\n    AND \"RevokedAtUtc\" IS NOT NULL\n    AND length(\"RevocationReason\") > 0)");
                });

            migrationBuilder.CreateTable(
                name: "IntegrationAuditLogEntries",
                schema: "integrations",
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
                    table.PrimaryKey("PK_IntegrationAuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                schema: "integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PayloadSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                    table.CheckConstraint("CK_WebhookDeliveries_AttemptCount", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_WebhookDeliveries_DeliveredAfterCreation", "\"DeliveredAtUtc\" IS NULL OR \"DeliveredAtUtc\" >= \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_WebhookDeliveries_LockShape", "\"LockedUntilUtc\" IS NULL OR (\"LastAttemptAtUtc\" IS NOT NULL AND \"LockedUntilUtc\" > \"LastAttemptAtUtc\")");
                    table.CheckConstraint("CK_WebhookDeliveries_PayloadSha256", "length(\"PayloadSha256\") = 64");
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                schema: "integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EventTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SigningSecretHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                    table.CheckConstraint("CK_WebhookSubscriptions_RevocationShape", "(\"Status\" IN ('Active', 'Paused')\n    AND \"RevokedByUserAccountId\" IS NULL\n    AND \"RevokedAtUtc\" IS NULL\n    AND length(\"RevocationReason\") = 0)\nOR\n(\"Status\" = 'Revoked'\n    AND \"RevokedByUserAccountId\" IS NOT NULL\n    AND \"RevokedAtUtc\" IS NOT NULL\n    AND length(\"RevocationReason\") > 0)");
                    table.CheckConstraint("CK_WebhookSubscriptions_SigningSecretHashSha256", "length(\"SigningSecretHashSha256\") = 64");
                    table.CheckConstraint("CK_WebhookSubscriptions_TargetHttps", "\"TargetUrl\" LIKE 'https://%'");
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationApiClients_KeyHashSha256",
                schema: "integrations",
                table: "IntegrationApiClients",
                column: "KeyHashSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationApiClients_TenantId_KeyPrefix",
                schema: "integrations",
                table: "IntegrationApiClients",
                columns: new[] { "TenantId", "KeyPrefix" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationApiClients_TenantId_Status_CreatedAtUtc",
                schema: "integrations",
                table: "IntegrationApiClients",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuditLogEntries_TenantId_OccurredAtUtc",
                schema: "integrations",
                table: "IntegrationAuditLogEntries",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_Status_CreatedAtUtc",
                schema: "integrations",
                table: "WebhookDeliveries",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_TenantId_CorrelationId_EventType",
                schema: "integrations",
                table: "WebhookDeliveries",
                columns: new[] { "TenantId", "CorrelationId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_TenantId_SubscriptionId_Status",
                schema: "integrations",
                table: "WebhookDeliveries",
                columns: new[] { "TenantId", "SubscriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_TenantId_Status_CreatedAtUtc",
                schema: "integrations",
                table: "WebhookSubscriptions",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_TenantId_TargetUrl",
                schema: "integrations",
                table: "WebhookSubscriptions",
                columns: new[] { "TenantId", "TargetUrl" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationApiClients",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "IntegrationAuditLogEntries",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions",
                schema: "integrations");
        }
    }
}
