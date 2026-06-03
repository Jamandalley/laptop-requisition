using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopRequisition.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeLockoutEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 20, 43, 17, 462, DateTimeKind.Utc).AddTicks(6129), new DateTime(2026, 6, 1, 20, 43, 17, 462, DateTimeKind.Utc).AddTicks(6134) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 20, 43, 17, 462, DateTimeKind.Utc).AddTicks(7818), new DateTime(2026, 6, 1, 20, 43, 17, 462, DateTimeKind.Utc).AddTicks(7819) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 0, 29, 16, 344, DateTimeKind.Utc).AddTicks(3592), new DateTime(2026, 6, 1, 0, 29, 16, 344, DateTimeKind.Utc).AddTicks(3595) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 0, 29, 16, 344, DateTimeKind.Utc).AddTicks(4569), new DateTime(2026, 6, 1, 0, 29, 16, 344, DateTimeKind.Utc).AddTicks(4569) });
        }
    }
}
