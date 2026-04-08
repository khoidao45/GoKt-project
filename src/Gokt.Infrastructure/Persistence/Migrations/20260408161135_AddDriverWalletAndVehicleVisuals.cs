using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gokt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverWalletAndVehicleVisuals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Vehicles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeatCount",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverDailyKpis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevenueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DailyRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsQualified = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedRate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculatedPay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDailyKpis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDailyKpis_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverWallets_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RevenueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverWalletTransactions_DriverWallets_DriverWalletId",
                        column: x => x.DriverWalletId,
                        principalTable: "DriverWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverWalletTransactions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 11, 34, 570, DateTimeKind.Utc).AddTicks(6448));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 11, 34, 570, DateTimeKind.Utc).AddTicks(6455));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 11, 34, 570, DateTimeKind.Utc).AddTicks(6497));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 11, 34, 570, DateTimeKind.Utc).AddTicks(6499));

            migrationBuilder.CreateIndex(
                name: "IX_DriverDailyKpis_DriverId_RevenueDate",
                table: "DriverDailyKpis",
                columns: new[] { "DriverId", "RevenueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverDailyKpis_RevenueDate",
                table: "DriverDailyKpis",
                column: "RevenueDate");

            migrationBuilder.CreateIndex(
                name: "IX_DriverWallets_DriverId",
                table: "DriverWallets",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverWalletTransactions_DriverId",
                table: "DriverWalletTransactions",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverWalletTransactions_DriverId_RevenueDate",
                table: "DriverWalletTransactions",
                columns: new[] { "DriverId", "RevenueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverWalletTransactions_DriverWalletId",
                table: "DriverWalletTransactions",
                column: "DriverWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverWalletTransactions_RevenueDate",
                table: "DriverWalletTransactions",
                column: "RevenueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverDailyKpis");

            migrationBuilder.DropTable(
                name: "DriverWalletTransactions");

            migrationBuilder.DropTable(
                name: "DriverWallets");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SeatCount",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Vehicles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 5, 3, 29, 38, 824, DateTimeKind.Utc).AddTicks(5474));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 5, 3, 29, 38, 824, DateTimeKind.Utc).AddTicks(5478));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 5, 3, 29, 38, 824, DateTimeKind.Utc).AddTicks(5480));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 5, 3, 29, 38, 824, DateTimeKind.Utc).AddTicks(5486));
        }
    }
}
