using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gokt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVehicleTypeCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000001"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 1.00m, 2.00m, 0.55m, 0.08m, "ElectricBike" });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000002"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 1.80m, 3.50m, 0.95m, 0.18m, "Seat4" });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000003"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate" },
                values: new object[] { 4.00m, 9.00m, 2.20m, 0.35m });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000004"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 2.40m, 5.50m, 1.25m, 0.22m, "Seat7" });

            migrationBuilder.InsertData(
                table: "PricingRules",
                columns: new[] { "Id", "BaseFare", "CreatedAt", "IsActive", "MinimumFare", "PerKmRate", "PerMinuteRate", "SurgeMultiplier", "VehicleType" },
                values: new object[] { new Guid("b1b1b1b1-0000-0000-0000-000000000005"), 2.90m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 6.50m, 1.45m, 0.25m, 1.0m, "Seat9" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 33, 3, 70, DateTimeKind.Utc).AddTicks(7563));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 33, 3, 70, DateTimeKind.Utc).AddTicks(7568));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 33, 3, 70, DateTimeKind.Utc).AddTicks(7569));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 33, 3, 70, DateTimeKind.Utc).AddTicks(7571));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000005"));

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000001"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 1.50m, 3.00m, 0.80m, 0.15m, "Economy" });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000002"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 2.00m, 5.00m, 1.20m, 0.20m, "Comfort" });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000003"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate" },
                values: new object[] { 3.00m, 8.00m, 2.00m, 0.30m });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000004"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 2.50m, 6.00m, 1.50m, 0.25m, "XL" });

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
        }
    }
}
