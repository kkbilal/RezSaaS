using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClosureNoticeDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountClosureCases_EligibilityAfterProposal",
                schema: "admin",
                table: "AccountClosureCases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountClosureCases_ExecutionAfterEligibility",
                schema: "admin",
                table: "AccountClosureCases");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EligibleForExecutionAtUtc",
                schema: "admin",
                table: "AccountClosureCases",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerNoticeDeliveredAtUtc",
                schema: "admin",
                table: "AccountClosureCases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE admin."AccountClosureCases"
                SET "CustomerNoticeDeliveredAtUtc" = "ProposedAtUtc"
                WHERE "EligibleForExecutionAtUtc" IS NOT NULL
                    AND "Status" IN ('Executing', 'Executed');

                UPDATE admin."AccountClosureCases"
                SET "CustomerNoticeDeliveredAtUtc" = NULL,
                    "EligibleForExecutionAtUtc" = NULL
                WHERE "Status" NOT IN ('Executing', 'Executed');
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountClosureCases_ExecutionAfterEligibility",
                schema: "admin",
                table: "AccountClosureCases",
                sql: "\"ExecutionStartedAtUtc\" IS NULL\nOR\n(\"EligibleForExecutionAtUtc\" IS NOT NULL\n    AND \"ExecutionStartedAtUtc\" >= \"EligibleForExecutionAtUtc\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountClosureCases_NoticeDeliveryShape",
                schema: "admin",
                table: "AccountClosureCases",
                sql: "(\"CustomerNoticeDeliveredAtUtc\" IS NULL\n    AND \"EligibleForExecutionAtUtc\" IS NULL)\nOR\n(\"CustomerNoticeDeliveredAtUtc\" IS NOT NULL\n    AND \"CustomerNoticeDeliveredAtUtc\" >= \"ProposedAtUtc\"\n    AND \"EligibleForExecutionAtUtc\" > \"CustomerNoticeDeliveredAtUtc\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountClosureCases_ExecutionAfterEligibility",
                schema: "admin",
                table: "AccountClosureCases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountClosureCases_NoticeDeliveryShape",
                schema: "admin",
                table: "AccountClosureCases");

            migrationBuilder.Sql(
                """
                UPDATE admin."AccountClosureCases"
                SET "EligibleForExecutionAtUtc" = "ProposedAtUtc" + INTERVAL '7 days'
                WHERE "EligibleForExecutionAtUtc" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "CustomerNoticeDeliveredAtUtc",
                schema: "admin",
                table: "AccountClosureCases");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EligibleForExecutionAtUtc",
                schema: "admin",
                table: "AccountClosureCases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountClosureCases_EligibilityAfterProposal",
                schema: "admin",
                table: "AccountClosureCases",
                sql: "\"EligibleForExecutionAtUtc\" > \"ProposedAtUtc\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountClosureCases_ExecutionAfterEligibility",
                schema: "admin",
                table: "AccountClosureCases",
                sql: "\"ExecutionStartedAtUtc\" IS NULL OR \"ExecutionStartedAtUtc\" >= \"EligibleForExecutionAtUtc\"");
        }
    }
}
