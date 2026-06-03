using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LaptopRequisition.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedEmployeeAndBackendEngineerRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(2116), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(2119) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3476), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3477) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3484), "Standard employee with limited access", "Employee", new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3485) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3490), "Engineer specializing in backend development", "Backend Engineer", new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3490) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

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
    }
}
