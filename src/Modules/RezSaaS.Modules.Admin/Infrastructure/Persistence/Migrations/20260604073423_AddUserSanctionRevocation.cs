using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezSaaS.Modules.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSanctionRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                schema: "admin",
                table: "UserSanctions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAtUtc",
                schema: "admin",
                table: "UserSanctions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedByUserAccountId",
                schema: "admin",
                table: "UserSanctions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevocationReason",
                schema: "admin",
                table: "UserSanctions");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                schema: "admin",
                table: "UserSanctions");

            migrationBuilder.DropColumn(
                name: "RevokedByUserAccountId",
                schema: "admin",
                table: "UserSanctions");
        }
    }
}
