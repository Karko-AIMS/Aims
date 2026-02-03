using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aims.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsAndVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrgId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Vin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PlateNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ModelYear = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicles_organizations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicles_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicles_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "Id", "CreatedAtUtc", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 2, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Default Org" });

            migrationBuilder.CreateIndex(
                name: "IX_users_OrgId",
                table: "users",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_CreatedByUserId",
                table: "vehicles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_OrgId_PlateNumber",
                table: "vehicles",
                columns: new[] { "OrgId", "PlateNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_OrgId_VehicleCode",
                table: "vehicles",
                columns: new[] { "OrgId", "VehicleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_OrgId_Vin",
                table: "vehicles",
                columns: new[] { "OrgId", "Vin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_UpdatedByUserId",
                table: "vehicles",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_organizations_OrgId",
                table: "users",
                column: "OrgId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_organizations_OrgId",
                table: "users");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_users_OrgId",
                table: "users");
        }
    }
}
