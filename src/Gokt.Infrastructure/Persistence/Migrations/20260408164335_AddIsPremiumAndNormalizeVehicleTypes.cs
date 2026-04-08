using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gokt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPremiumAndNormalizeVehicleTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000005"));

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "RideRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "RideRequests"
                SET "IsPremium" = TRUE
                WHERE "RequestedVehicleType" = 'Premium';
                """);

            migrationBuilder.Sql("""
                UPDATE "RideRequests"
                SET "RequestedVehicleType" = 'Seat4'
                WHERE "RequestedVehicleType" IN ('Economy', 'Comfort', 'Premium');
                """);

            migrationBuilder.Sql("""
                UPDATE "RideRequests"
                SET "RequestedVehicleType" = 'Seat7'
                WHERE "RequestedVehicleType" = 'XL';
                """);

            migrationBuilder.Sql("""
                UPDATE "Vehicles"
                SET "VehicleType" = 'Seat4'
                WHERE "VehicleType" IN ('Economy', 'Comfort', 'Premium');
                """);

            migrationBuilder.Sql("""
                UPDATE "Vehicles"
                SET "VehicleType" = 'Seat7'
                WHERE "VehicleType" = 'XL';
                """);

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000004"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 2.90m, 6.50m, 1.45m, 0.25m, "Seat9" });

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000003"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 2.40m, 5.50m, 1.25m, 0.22m, "Seat7" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 43, 35, 417, DateTimeKind.Utc).AddTicks(383));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 43, 35, 417, DateTimeKind.Utc).AddTicks(387));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 43, 35, 417, DateTimeKind.Utc).AddTicks(396));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 8, 16, 43, 35, 417, DateTimeKind.Utc).AddTicks(397));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "RideRequests");

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-0000-0000-0000-000000000003"),
                columns: new[] { "BaseFare", "MinimumFare", "PerKmRate", "PerMinuteRate", "VehicleType" },
                values: new object[] { 4.00m, 9.00m, 2.20m, 0.35m, "Premium" });

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
    }
}
