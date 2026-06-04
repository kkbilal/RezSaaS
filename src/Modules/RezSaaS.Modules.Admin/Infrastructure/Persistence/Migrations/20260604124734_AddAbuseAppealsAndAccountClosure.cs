using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbuseAppealsAndAccountClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbuseAppeals",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Statement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbuseAppeals", x => x.Id);
                    table.CheckConstraint("CK_AbuseAppeals_ReviewShape", "(\"Status\" = 'PendingReview'\n    AND \"ReviewedAtUtc\" IS NULL\n    AND \"ReviewedByUserAccountId\" IS NULL\n    AND \"ReviewReason\" IS NULL)\nOR\n(\"Status\" IN ('Accepted', 'Rejected')\n    AND \"ReviewedAtUtc\" IS NOT NULL\n    AND \"ReviewedAtUtc\" >= \"CreatedAtUtc\"\n    AND \"ReviewedByUserAccountId\" IS NOT NULL\n    AND \"ReviewReason\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "AccountClosureCases",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomerNotice = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EligibleForExecutionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExecutedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutionStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionStartedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    InternalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProposedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProposedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountClosureCases", x => x.Id);
                    table.CheckConstraint("CK_AccountClosureCases_CompletionAfterExecutionStart", "\"ExecutedAtUtc\" IS NULL OR \"ExecutedAtUtc\" >= \"ExecutionStartedAtUtc\"");
                    table.CheckConstraint("CK_AccountClosureCases_DecisionAfterProposal", "\"DecidedAtUtc\" IS NULL OR \"DecidedAtUtc\" >= \"ProposedAtUtc\"");
                    table.CheckConstraint("CK_AccountClosureCases_DecisionShape", "(\"Status\" = 'PendingApproval'\n    AND \"ReviewedByUserAccountId\" IS NULL\n    AND \"DecisionReason\" IS NULL\n    AND \"DecidedAtUtc\" IS NULL)\nOR\n(\"Status\" IN ('Approved', 'Rejected', 'CancelledByAppeal', 'Executing', 'Executed')\n    AND \"ReviewedByUserAccountId\" IS NOT NULL\n    AND \"DecisionReason\" IS NOT NULL\n    AND \"DecidedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_AccountClosureCases_EligibilityAfterProposal", "\"EligibleForExecutionAtUtc\" > \"ProposedAtUtc\"");
                    table.CheckConstraint("CK_AccountClosureCases_ExecutionAfterEligibility", "\"ExecutionStartedAtUtc\" IS NULL OR \"ExecutionStartedAtUtc\" >= \"EligibleForExecutionAtUtc\"");
                    table.CheckConstraint("CK_AccountClosureCases_ExecutionShape", "(\"Status\" NOT IN ('Executing', 'Executed')\n    AND \"ExecutionStartedByUserAccountId\" IS NULL\n    AND \"ExecutionStartedAtUtc\" IS NULL\n    AND \"ExecutedByUserAccountId\" IS NULL\n    AND \"ExecutedAtUtc\" IS NULL)\nOR\n(\"Status\" = 'Executing'\n    AND \"ExecutionStartedByUserAccountId\" IS NOT NULL\n    AND \"ExecutionStartedAtUtc\" IS NOT NULL\n    AND \"ExecutedByUserAccountId\" IS NULL\n    AND \"ExecutedAtUtc\" IS NULL)\nOR\n(\"Status\" = 'Executed'\n    AND \"ExecutionStartedByUserAccountId\" IS NOT NULL\n    AND \"ExecutionStartedAtUtc\" IS NOT NULL\n    AND \"ExecutedByUserAccountId\" IS NOT NULL\n    AND \"ExecutedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_AccountClosureCases_NoSelfProposal", "\"UserAccountId\" <> \"ProposedByUserAccountId\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbuseAppeals_UserAccountId_Status_CreatedAtUtc",
                schema: "admin",
                table: "AbuseAppeals",
                columns: new[] { "UserAccountId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AbuseAppeals_UserAccountId_TargetType_TargetId",
                schema: "admin",
                table: "AbuseAppeals",
                columns: new[] { "UserAccountId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountClosureCases_Status_EligibleForExecutionAtUtc",
                schema: "admin",
                table: "AccountClosureCases",
                columns: new[] { "Status", "EligibleForExecutionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountClosureCases_UserAccountId",
                schema: "admin",
                table: "AccountClosureCases",
                column: "UserAccountId",
                unique: true,
                filter: "\"Status\" IN ('PendingApproval', 'Approved', 'Executing')");

            migrationBuilder.CreateIndex(
                name: "IX_AccountClosureCases_UserAccountId_Status_ProposedAtUtc",
                schema: "admin",
                table: "AccountClosureCases",
                columns: new[] { "UserAccountId", "Status", "ProposedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbuseAppeals",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "AccountClosureCases",
                schema: "admin");
        }
    }
}
