using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aims.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultOrgSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAtUtc",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Name",
                table: "organizations",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_organizations_Name",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "DeactivatedAtUtc",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "organizations");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "Id", "CreatedAtUtc", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 2, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Default Org" });
        }
    }
}
