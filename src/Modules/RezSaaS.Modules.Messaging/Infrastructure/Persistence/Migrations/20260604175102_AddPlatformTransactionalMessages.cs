using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Messaging.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTransactionalMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformTransactionalMessages",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveryKey = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformTransactionalMessages", x => x.Id);
                    table.CheckConstraint("CK_PlatformTransactionalMessages_AttemptAfterCreation", "\"LastAttemptAtUtc\" IS NULL OR \"LastAttemptAtUtc\" >= \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_AttemptCount", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_CompletionAfterCreation", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_DeliveryAfterCreation", "\"SentAtUtc\" IS NULL OR \"SentAtUtc\" >= \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_LockAfterAttempt", "\"LockedUntilUtc\" IS NULL OR (\"LastAttemptAtUtc\" IS NOT NULL AND \"LockedUntilUtc\" > \"LastAttemptAtUtc\")");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_NextAttemptAfterCreation", "\"NextAttemptAtUtc\" IS NULL OR \"NextAttemptAtUtc\" >= \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_SentShape", "\"Status\" <> 'Sent' OR \"SentAtUtc\" IS NOT NULL");
                    table.CheckConstraint("CK_PlatformTransactionalMessages_StateShape", "(\"Status\" = 'Pending'\n    AND \"NextAttemptAtUtc\" IS NOT NULL\n    AND \"LockedUntilUtc\" IS NULL\n    AND \"CompletedAtUtc\" IS NULL)\nOR\n(\"Status\" = 'Processing'\n    AND \"NextAttemptAtUtc\" IS NULL\n    AND \"LockedUntilUtc\" IS NOT NULL\n    AND \"CompletedAtUtc\" IS NULL)\nOR\n(\"Status\" IN ('Sent', 'Failed', 'Cancelled')\n    AND \"NextAttemptAtUtc\" IS NULL\n    AND \"LockedUntilUtc\" IS NULL\n    AND \"CompletedAtUtc\" IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTransactionalMessages_DeliveryKey",
                schema: "messaging",
                table: "PlatformTransactionalMessages",
                column: "DeliveryKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTransactionalMessages_Purpose_CorrelationId",
                schema: "messaging",
                table: "PlatformTransactionalMessages",
                columns: new[] { "Purpose", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTransactionalMessages_Status_NextAttemptAtUtc_Creat~",
                schema: "messaging",
                table: "PlatformTransactionalMessages",
                columns: new[] { "Status", "NextAttemptAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformTransactionalMessages",
                schema: "messaging");
        }
    }
}
